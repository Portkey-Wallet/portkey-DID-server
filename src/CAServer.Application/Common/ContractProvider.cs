using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using CAServer.ClaimToken.Dtos;
using CAServer.Options;
using CAServer.Signature;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Portkey.Contracts.TokenClaim;
using Volo.Abp.DependencyInjection;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;
using ChainInfo = CAServer.Grains.Grain.ApplicationHandler.ChainInfo;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
    public Task<SendTransactionOutput> TransferAsync(string chainId, ClaimTokenRequestDto claimTokenRequestDto);
    public Task ClaimTokenAsync(string chainId, string symbol);

    Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        bool isTokenContract, ChainInfo chainInfo) where T : class, IMessage<T>, new();

    Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        bool isTokenContract, string chainId) where T : class, IMessage<T>, new();
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly ISignatureProvider _signatureProvider;
    // CommonPrivateKeyForCallTx: Just for query, no assets and no permissions
    private const string CommonPrivateKeyForCallTx = "5ed86f0a0203a1b15410834a01fce0df0c2bd8b1b7f6ccc5f165cd97f8978517";

    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        ISignatureProvider signatureProvider, IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureProvider = signatureProvider;
    }


    public async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        string addressFromPrivateKey = client.GetAddressFromPrivateKey(CommonPrivateKeyForCallTx);

        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(addressFromPrivateKey,
            chainOption.ContractAddress, MethodName.GetVerifierServers, param);

        _logger.LogDebug("param is: {transaction}, publicKey is:{publicKey} ", transaction, chainOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(addressFromPrivateKey, transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });
        return GetVerifierServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }

    public async Task<SendTransactionOutput> TransferAsync(string chainId, ClaimTokenRequestDto claimTokenRequestDto)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return new SendTransactionOutput();
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(_claimTokenInfoOption.PublicKey);
        var param = new TransferInput
        {
            Symbol = claimTokenRequestDto.Symbol,
            Amount = long.Parse(claimTokenRequestDto.Amount),
            To = Address.FromBase58(claimTokenRequestDto.Address)
        };
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.TokenContractAddress, MethodName.Transfer, param);

        _logger.LogDebug("param is: {transaction}, publicKey is:{publicKey} ", transaction,
            _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.ToByteArray().ToHex());

        var result = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign
        });
        return result;
    }

    public async Task ClaimTokenAsync(string chainId, string symbol)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(_claimTokenInfoOption.PublicKey);
        var param = new ClaimTokenInput
        {
            Symbol = symbol,
            Amount = _claimTokenInfoOption.ClaimTokenAmount
        };
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            _claimTokenInfoOption.ClaimTokenAddress, MethodName.ClaimToken, param);

        _logger.LogDebug("param is: {transaction}, publicKey is:{publicKey} ", transaction,
            _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.ToByteArray().ToHex());

        await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign
        });
    }

    public async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, bool isTokenContract,
        ChainInfo chainInfo)
        where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(CommonPrivateKeyForCallTx);
        var contractAddress = isTokenContract ? chainInfo.TokenContractAddress : chainInfo.ContractAddress;

        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);

        var txWithSign = await _signatureProvider.SignTxMsg(addressFromPrivateKey, transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    public async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, bool isTokenContract,
        string chainId)
        where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(CommonPrivateKeyForCallTx);
        var contractAddress = isTokenContract ? chainInfo.TokenContractAddress : chainInfo.ContractAddress;

        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);

        var txWithSign = await _signatureProvider.SignTxMsg(addressFromPrivateKey, transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }
}