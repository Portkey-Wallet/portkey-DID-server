using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Proto;
using CAServer.CAAccount.Provider.TonWallet.Extensions;
using CAServer.CAAccount.TonWallet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using TonLibDotNet;
using TonLibDotNet.Cells;
using TonProof.Types;
using Volo.Abp;
using Volo.Abp.Auditing;
using Hash = AElf.Types.Hash;

namespace CAServer.CAAccount.Provider.TonWallet;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TonWalletProvider : CAServerAppService, ITonWalletProvider
{
    private readonly ILogger<TonWalletProvider> _logger;
    // private readonly ITonClient tonClient;
    // private readonly IPublicKeyProvider publicKeyProvider;
    private readonly TonProofOptions _options;

    private readonly byte[] _tonConnectPrefixBytes;
    private readonly byte[] _tonProofPrefixBytes;
    
    public TonWalletProvider(ILogger<TonWalletProvider> logger,
        IOptions<TonProofOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _tonConnectPrefixBytes = Encoding.UTF8.GetBytes(this._options.TonConnectPrefix);
        _tonProofPrefixBytes = Encoding.UTF8.GetBytes(this._options.TonProofPrefix);
    }
    
    public async Task<VerifyResult> VerifyAsync(CheckProofRequest request, CancellationToken cancellationToken = default)
    {
        var requestRaw = new CheckProofRequestRaw(request);

        if (!requestRaw.Workchain.HasValue)
        {
            _logger.LogDebug(
                "Address {Address} is invalid. The address provided lacks the correct format and omits a workchain",
                request.Address);
            return VerifyResult.InvalidAddress;
        }

        if (requestRaw.InitState is null)
        {
            _logger.LogDebug(
                "The InitState {InitState} structure is invalid. This could indicate that the contract is not a well-known wallet",
                request.Proof.StateInit);
            return VerifyResult.InvalidInitState;
        }

        // var isParsed = this.TryParseWalletPublicKey(requestRaw.InitState.Code, requestRaw.Data, out var walletPublicKey);

        // var publicKey = isParsed
        //     ? walletPublicKey
        //     : await this.publicKeyProvider.GetPublicKeyAsync(requestRaw.Address, cancellationToken);
        //
        // if (!requestRaw.PublicKey.Equals(publicKey, StringComparison.OrdinalIgnoreCase))
        // {
        //     _logger.LogDebug(
        //         "Public key mismatch: provided public key {ProvidedPk} does not match the parsed or retrieved public key {RetrievedPk}",
        //         requestRaw.PublicKey,
        //         publicKey);
        //     return VerifyResult.PublicKeyMismatch;
        // }

        // var wantedAddress = await this.tonClient.GetAccountAddress(requestRaw.InitState).ConfigureAwait(false);
        // if (!AddressUtils.Instance.AddressEquals(wantedAddress.Value, requestRaw.AddressBytes))
        // {
        //     _logger.LogDebug(
        //         "Address mismatch: expected address {WantedAddress}, but got {Address}",
        //         wantedAddress.Value,
        //         requestRaw.Address);
        //     return VerifyResult.AddressMismatch;
        // }

        // if (!this.options.AllowedDomains.Contains(requestRaw.Proof.Domain.Value))
        // {
        //     _logger.LogDebug("Domain not allowed: {Domain} is not in the list of allowed domains",
        //         requestRaw.Proof.Domain.Value);
        //     return VerifyResult.DomainNotAllowed;
        // }

        // var dateTime = DateTimeOffset.FromUnixTimeSeconds(requestRaw.Proof.Timestamp).UtcDateTime;
        // var proofDatetime = dateTime.AddSeconds(this.options.ValidAuthTime);
        // if (proofDatetime < DateTime.UtcNow)
        // {
        //     _logger.LogDebug(
        //         "Proof expired: the proof DateTimes {ProofDateTime} is outside the allowed validity period: {ValidAuthTime} sec.",
        //         proofDatetime,
        //         this.options.ValidAuthTime);
        //     return VerifyResult.ProofExpired;
        // }

        var msg = this.CreateMessage(requestRaw);
        var msgHash = SHA256.HashData(msg);
        _logger.LogInformation("msgHash:{0}", msgHash);
        var algorithm = SignatureAlgorithm.Ed25519;

        var pKey = PublicKey.Import(
            algorithm,
            Convert.FromHexString(requestRaw.PublicKey).AsSpan(),
            KeyBlobFormat.RawPublicKey);
        _logger.LogInformation("pKey:{0}", Convert.FromHexString(requestRaw.PublicKey));
        _logger.LogInformation("signature:{0}", Convert.FromBase64String(requestRaw.Proof.Signature));
        var result = algorithm.Verify(
            pKey,
            msgHash,
            Convert.FromBase64String(requestRaw.Proof.Signature).AsSpan());

        return result ? VerifyResult.Valid : VerifyResult.HashMismatch;
    }

    public string GetTonWalletMessage(CheckProofRequest request)
    {
        var requestRaw = new CheckProofRequestRaw(request);
        var msg = this.CreateMessage(requestRaw);
        var msgHash = SHA256.HashData(msg);
        _logger.LogInformation("GetTonWalletMessage msgHash:{0}, msgHashToHex:{1}", msgHash, msgHash.ToHex());
        return msgHash.ToHex();
    }

    private byte[] CreateMessage(CheckProofRequestRaw request)
    {
        Span<byte> wc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(wc, request.Workchain!.Value);

        Span<byte> ts = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(ts, (ulong)request.Proof.Timestamp);

        Span<byte> dl = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(dl, request.Proof.Domain.LengthBytes);

        var domainBytes = Encoding.UTF8.GetBytes(request.Proof.Domain.Value);
        var payloadBytes = Encoding.UTF8.GetBytes(request.Proof.Payload);

        var totalLength = this._tonProofPrefixBytes.Length +
                          wc.Length + request.AddressBytes.Length +
                          dl.Length + domainBytes.Length +
                          ts.Length +
                          payloadBytes.Length;

        // message = utf8_encode("ton-proof-item-v2/") ++
        //   Address ++
        //   AppDomain ++
        //   Timestamp ++
        //   Payload

        var message = new byte[totalLength].AsSpan();
        var offset = 0;

        message.CopyFrom(this._tonProofPrefixBytes, ref offset);
        message.CopyFrom(wc, ref offset);
        message.CopyFrom(request.AddressBytes, ref offset);
        message.CopyFrom(dl, ref offset);
        message.CopyFrom(domainBytes, ref offset);
        message.CopyFrom(ts, ref offset);
        message.CopyFrom(payloadBytes, ref offset);

        var msgHash = SHA256.HashData(message);

        // fullMessage = 0xffff ++
        //  utf8_encode("ton-connect") ++
        //  sha256(message)

        ReadOnlySpan<byte> ff = stackalloc byte[2] { 0xFF, 0xFF };
        message = new byte[ff.Length + this._tonConnectPrefixBytes.Length + msgHash.Length].AsSpan();
        offset = 0;

        message.CopyFrom(ff, ref offset);
        message.CopyFrom(this._tonConnectPrefixBytes, ref offset);
        message.CopyFrom(msgHash, ref offset);

        return message.ToArray();
    }

    private bool TryParseWalletPublicKey(string code, Cell data, [NotNullWhen(true)] out string publicKey)
    {
        // if (this._options.KnownWallets.TryGetValue(code, out var knownWallet))
        // {
        //     var wallet = knownWallet.Invoke();
        //     publicKey = wallet.LoadPublicKey(data);
        //     return true;
        // }

        _logger.LogDebug("Failed to parse wallet publicKey for unknown code: {Code}", code);

        publicKey = null;
        return false;
    }
}