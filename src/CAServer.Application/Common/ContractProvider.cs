using System.Collections.Generic;
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
    public Task ClaimTokenAsync(string symbol, string address, string chainId);
    public Task<SendTransactionOutput> SendTransferAsync(string symbol, string amount, string address, string chainId);

    public Task<SendTransactionOutput> SendRawTransaction(string chainId, string rawTransaction);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly ISignatureProvider _signatureProvider;
    private readonly ContractOptions _contractOptions;

    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        ISignatureProvider signatureProvider, IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption,
        IOptionsSnapshot<ContractOptions> contractOptions)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureProvider = signatureProvider;
        _contractOptions = contractOptions.Value;
    }


    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, string contractAddress,
        string chainId) where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(_contractOptions.CommonPrivateKeyForCallTx);

        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(_contractOptions.CommonPrivateKeyForCallTx, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    private async Task<SendTransactionOutput> SendTransactionAsync<T>(string methodName, IMessage param,
        string senderPubKey, string contractAddress, string chainId)
        where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(senderPubKey);

        var transaction = await client.GenerateTransactionAsync(ownAddress, contractAddress, methodName, param);

        _logger.LogDebug("Send tx methodName is: {methodName} param is: {transaction}, publicKey is:{publicKey} ",
            methodName, transaction, _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());

        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });

        return result;
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoAsync(Hash caHash, Hash loginGuardianIdentifierHash,
        string chainId)
    {
        var param = new GetHolderInfoInput();
        param.CaHash = caHash;
        param.LoginGuardianIdentifierHash = loginGuardianIdentifierHash;
        var output = await CallTransactionAsync<GetHolderInfoOutput>(AElfContractMethodName.GetHolderInfo, param,
            _chainOptions.ChainInfos[chainId].ContractAddress, chainId);
        return output;
    }

    public async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out _))
        {
            return null;
        }

        return await CallTransactionAsync<GetVerifierServersOutput>(AElfContractMethodName.GetVerifierServers,
            new Empty(), _chainOptions.ChainInfos[chainId].ContractAddress, chainId);
    }

    public async Task<GetBalanceOutput> GetBalanceAsync(string symbol, string address, string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out _))
        {
            return null;
        }

        var getBalanceParam = new GetBalanceInput
        {
            Symbol = symbol,
            Owner = Address.FromBase58(address)
        };

        return await CallTransactionAsync<GetBalanceOutput>(AElfContractMethodName.GetBalance, getBalanceParam,
            _chainOptions.ChainInfos[chainId].TokenContractAddress, chainId);
    }

    public async Task ClaimTokenAsync(string symbol, string address, string chainId)
    {
        var claimTokenParam = new ClaimTokenInput
        {
            Symbol = symbol,
            Amount = _claimTokenInfoOption.ClaimTokenAmount
        };
        await SendTransactionAsync<ClaimTokenInput>(AElfContractMethodName.ClaimToken, claimTokenParam,
            _claimTokenInfoOption.PublicKey, address, chainId);
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

        return await SendTransactionAsync<TransferInput>(AElfContractMethodName.Transfer, transferParam,
            _claimTokenInfoOption.PublicKey, _chainOptions.ChainInfos[chainId].TokenContractAddress, chainId);
    }

    public async Task<SendTransactionOutput> SendRawTransaction(string chainId, string rawTransaction)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = rawTransaction
        });

        return result;
    }
}