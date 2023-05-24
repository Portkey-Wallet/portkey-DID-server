using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Signature;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Portkey.Contracts.TokenClaim;
using Volo.Abp.DependencyInjection;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetHolderInfoOutput> GetHolderInfoAsync(Hash caHash, Hash loginGuardianIdentifierHash, string chainId);
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
    public Task<GetBalanceOutput> GetBalanceAsync(string symbol, string address, string chainId);
    public Task ClaimTokenAsync(string symbol, string chainId);
    public Task<SendTransactionOutput> SendTransferAsync(string symbol, string amount, string address, string chainId);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly ISignatureProvider _signatureProvider;

    // CommonPrivateKeyForCallTx: Just for query, no assets and no permissions
    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        ISignatureProvider signatureProvider, IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureProvider = signatureProvider;
    }


    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, bool isTokenContract,
        string chainId) where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(AElfContractKeyConst.CommonPrivateKeyForCallTx);
        var contractAddress = isTokenContract ? chainInfo.TokenContractAddress : chainInfo.ContractAddress;

        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(AElfContractKeyConst.CommonPrivateKeyForCallTx, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    private async Task<SendTransactionOutput> SendTransactionAsync<T>(string methodName, IMessage param, string chainId)
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

    public async Task<GetHolderInfoOutput> GetHolderInfoAsync(Hash caHash, Hash loginGuardianIdentifierHash,
        string chainId)
    {
        var param = new GetHolderInfoInput();
        param.CaHash = caHash;
        param.LoginGuardianIdentifierHash = loginGuardianIdentifierHash;
        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(AElfContractMethodName.GetHolderInfo, param, false,
                chainId);
        return output;
    }

    public async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        return await CallTransactionAsync<GetVerifierServersOutput>(AElfContractMethodName.GetVerifierServers,
            new Empty(), false, chainId);
    }

    public async Task<GetBalanceOutput> GetBalanceAsync(string symbol, string address, string chainId)
    {
        var getBalanceParam = new GetBalanceInput
        {
            Symbol = symbol,
            Owner = Address.FromBase58(address)
        };
        return await CallTransactionAsync<GetBalanceOutput>(AElfContractMethodName.GetBalance, getBalanceParam, true,
            chainId);
    }

    public async Task ClaimTokenAsync(string symbol, string chainId)
    {
        var claimTokenParam = new ClaimTokenInput
        {
            Symbol = symbol,
            Amount = _claimTokenInfoOption.ClaimTokenAmount
        };
        await SendTransactionAsync<ClaimTokenInput>(AElfContractMethodName.ClaimToken, claimTokenParam, chainId);
    }

    public async Task<SendTransactionOutput> SendTransferAsync(string symbol, string amount, string address,
        string chainId)
    {
        var transferParam = new TransferInput
        {
            Symbol = symbol,
            Amount = long.Parse(amount),
            To = Address.FromBase58(address)
        };

        return await SendTransactionAsync<TransferInput>(AElfContractMethodName.Transfer, transferParam, chainId);
    }
}