using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAServer.Options;
using CAServer.Signature;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Common;

public interface IContractProvider
{
    Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        bool isTokenContract, string chainId) where T : class, IMessage<T>, new();

    Task<SendTransactionOutput> SendTransactionAsync<T>(string methodName, IMessage param, string chainId)
        where T : class, IMessage<T>, new();
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


    public async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, bool isTokenContract,
        string chainId) where T : class, IMessage<T>, new()
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

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(CommonPrivateKeyForCallTx, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    public async Task<SendTransactionOutput> SendTransactionAsync<T>(string methodName, IMessage param, string chainId)
        where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(_claimTokenInfoOption.PublicKey);

        var transaction = await client.GenerateTransactionAsync(ownAddress, chainOption.TokenContractAddress,
            methodName, param);

        _logger.LogDebug("Send tx methodName is: {methodName} param is: {transaction}, publicKey is:{publicKey} ",
            methodName, transaction, _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.ToByteArray().ToHex());

        var result = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign
        });

        return result;
    }
}