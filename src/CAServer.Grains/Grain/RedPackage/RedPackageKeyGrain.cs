using System.Text;
using AElf;
using AElf.Cryptography;
using CAServer.Grains.State.RedPackage;

namespace CAServer.Grains.Grain.RedPackage;

public class RedPackageKeyGrain : Orleans.Grain<RedPackageKeyState>, IRedPackageKeyGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }
    
    public Task<string> GetPublicKey()
    {
        return Task.FromResult(State.PublicKey);
    }

    public async Task<string> GenerateKey()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        State.PublicKey = keyPair.PublicKey.ToHex();
        State.PrivateKey = keyPair.PrivateKey.ToHex();
        await WriteStateAsync();
        return State.PublicKey;
    }
    
    public Task<string> GenerateSignature(string input)
    {
        var hashByteArray = HashHelper.ComputeFrom(input).ToByteArray();
        var signature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(State.PrivateKey), hashByteArray);
        return Task.FromResult(signature.ToHex());
    }
    
    public async Task<Tuple<string, string>> GenerateKeyAndSignature(string input)
    {
        var publicKey = await GenerateKey();
        var signature = GenerateSignature(input);
        return Tuple.Create(publicKey, signature.Result);
    }

    public Task<bool> VerifySignature(string data, string sig)
    {
        var publicKey = ByteArrayHelper.HexStringToByteArray(State.PublicKey);
        var signature = ByteArrayHelper.HexStringToByteArray(sig);
        var dataBytes = Encoding.UTF8.GetBytes(data).ComputeHash();

        return Task.FromResult(CryptoHelper.VerifySignature(signature, dataBytes, publicKey));
    }
}