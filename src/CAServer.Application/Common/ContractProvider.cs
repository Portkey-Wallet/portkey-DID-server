using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.Commons;
using CAServer.ContractService;
using CAServer.Monitor;
using CAServer.Options;
using CAServer.Signature.Provider;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Portkey.Contracts.TokenClaim;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;
using GetTokenInfoInput = AElf.Client.MultiToken.GetTokenInfoInput;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetHolderInfoOutput> GetHolderInfoAsync(Hash caHash, Hash loginGuardianIdentifierHash, string chainId);
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
    public Task<BoolValue> VerifyZkLogin(string chainId, GuardianInfo guardianInfo, Hash caHash);
    public Task<BoolValue> VerifySignature(string chainId, GuardianInfo guardianInfo, string methodName, Hash caHash, string operationDetails);
    public Task<GetBalanceOutput> GetBalanceAsync(string symbol, string address, string chainId);
    public Task<GetAllowanceOutput> GetAllowanceAsync(string symbol, string owner, string spender, string chainId);
    public Task ClaimTokenAsync(string symbol, string address, string chainId);

    public Task<SendTransactionOutput> SendTransferAsync(string symbol, string amount, string address, string chainId,
        string fromPublicKey);

    Task<SendTransactionOutput> SendRawTransactionAsync(string chainId, string rawTransaction);
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);
    Task<ChainStatusDto> GetChainStatusAsync(string chainId);

    Task<Tuple<string, Transaction>> GenerateTransferTransactionAsync(string symbol, string amount, string address,
        string chainId, string senderPubKey);

    Task<TransactionResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeDto assignProjectDelegateeDto);

    Task<TokenInfo> GetTokenInfoAsync(string chainId, string symbol);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly ISignatureProvider _signatureProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IIndicatorScope _indicatorScope;
    private readonly ContractServiceProxy _contractServiceProxy;


    public ContractProvider(IOptionsMonitor<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        IClusterClient clusterClient,
        ISignatureProvider signatureProvider, IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption,
        IIndicatorScope indicatorScope,
        ContractServiceProxy contractServiceProxy)
    {
        _chainOptions = chainOptions.CurrentValue;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureProvider = signatureProvider;
        _indicatorScope = indicatorScope;
        _contractServiceProxy = contractServiceProxy;
        _clusterClient = clusterClient;
    }

    public async Task<TransactionResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeDto assignProjectDelegateeDto)
    {
        try
        {
            var result = await _contractServiceProxy.AuthorizeDelegateAsync(assignProjectDelegateeDto);

            _logger.LogInformation(
                "Authorize Delegate:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                result.TransactionId, result.BlockNumber, result.Status, result.Error);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorize Delegate Error: {input}",
                JsonConvert.SerializeObject(assignProjectDelegateeDto.ToString(), Formatting.Indented));
            return new TransactionResultDto();
        }
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

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(SignatureKeyHelp.CommonPrivateKeyForCallTx);
        var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.GenerateTransactionAsync.ToString());
        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);
        _indicatorScope.End(generateIndicator);

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(SignatureKeyHelp.CommonPrivateKeyForCallTx, transaction);

        var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.ExecuteTransactionAsync.ToString());
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        _indicatorScope.End(interIndicator);

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        var client = await GetAElfClientAsync(chainId);
        AssertHelper.NotNull(client, "Send RawTransaction FAILED!, client of ChainId={ChainId} NOT FOUND", chainId);
        return await client.GetChainStatusAsync();
    }

    /// <summary>
    ///     Generate transfer-transaction
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="amount"></param>
    /// <param name="address"></param>
    /// <param name="chainId"></param>
    /// <param name="senderPubKey"></param>
    /// <returns> Tuple( transactionId -> transaction ) </returns>
    public async Task<Tuple<string, Transaction>> GenerateTransferTransactionAsync(string symbol, string amount,
        string address,
        string chainId, string senderPubKey)
    {
        AssertHelper.IsTrue(_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo),
            "ChainInfo not found : " + chainId);
        var methodName = AElfContractMethodName.Transfer;
        var transferParam = new TransferInput
        {
            Symbol = symbol,
            Amount = long.Parse(amount),
            To = Address.FromBase58(address)
        };

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(senderPubKey);

        var transaction =
            await client.GenerateTransactionAsync(ownAddress, chainInfo.TokenContractAddress, methodName,
                transferParam);
        var transactionId = transaction.GetHash().ToHex();
        _logger.LogDebug("Send tx methodName is: {MethodName} param is: {Transaction}, publicKey is:{PublicKey} ",
            methodName, transaction, senderPubKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transactionId);
        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);
        return new Tuple<string, Transaction>(transactionId, transaction);
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

        var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.GenerateTransactionAsync.ToString());
        var transaction = await client.GenerateTransactionAsync(ownAddress, contractAddress, methodName, param);

        _indicatorScope.End(generateIndicator);
        _logger.LogDebug("Send tx methodName is: {methodName} param is: {transaction}, publicKey is:{publicKey} ",
            methodName, transaction, _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());

        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

        var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.SendTransactionAsync.ToString());

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
        _indicatorScope.End(interIndicator);
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

    public async Task<BoolValue> VerifyZkLogin(string chainId, Portkey.Contracts.CA.GuardianInfo guardianInfo, Hash caHash)
    {
        var param = new VerifyZkLoginRequest()
        {
            GuardianApproved = guardianInfo,
            CaHash = caHash
        };
        return await CallTransactionAsync<BoolValue>(
            AElfContractMethodName.VerifyZkLogin, param, _chainOptions.ChainInfos[chainId].ContractAddress, chainId);
    }

    public async Task<BoolValue> VerifySignature(string chainId, GuardianInfo guardianInfo, string methodName, Hash caHash,
        string operationDetails)
    {
        var param = new VerifySignatureRequest()
        {
            GuardianApproved = guardianInfo,
            MethodName = methodName,
            CaHash = caHash,
            OperationDetails = operationDetails
        };
        return await CallTransactionAsync<BoolValue>(
            AElfContractMethodName.VerifySignature, param, _chainOptions.ChainInfos[chainId].ContractAddress, chainId);
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

    public async Task<GetAllowanceOutput> GetAllowanceAsync(string symbol, string owner, string spender, string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out _))
        {
            return null;
        }

        var param = new GetAllowanceOutput
        {
            Symbol = symbol,
            Owner = Address.FromBase58(owner),
            Spender = Address.FromBase58(spender),
        };

        return await CallTransactionAsync<GetAllowanceOutput>(AElfContractMethodName.GetAllowance, param,
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
        string chainId, string fromPublicKey)
    {
        var transferParam = new TransferInput
        {
            Symbol = symbol,
            Amount = long.Parse(amount),
            To = Address.FromBase58(address)
        };

        return await SendTransactionAsync<TransferInput>(AElfContractMethodName.Transfer, transferParam,
            fromPublicKey, _chainOptions.ChainInfos[chainId].TokenContractAddress, chainId);
    }

    public async Task<SendTransactionOutput> SendRawTransactionAsync(string chainId, string rawTransaction)
    {
        var client = await GetAElfClientAsync(chainId);
        if (client == null)
            throw new UserFriendlyException("Send RawTransaction FAILED!, client of ChainId={ChainId} NOT FOUND");

        var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.SendTransactionAsync.ToString());

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = rawTransaction
        });

        _indicatorScope.End(generateIndicator);

        return result;
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        var client = await GetAElfClientAsync(chainId);

        var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.GetTransactionResultAsync.ToString());
        var result = await client.GetTransactionResultAsync(transactionId);

        _indicatorScope.End(generateIndicator);
        return result;
    }

    public async Task<TokenInfo> GetTokenInfoAsync(string chainId, string symbol)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out _))
        {
            return null;
        }

        var getTokenInfoParam = new GetTokenInfoInput()
        {
            Symbol = symbol
        };

        return await CallTransactionAsync<TokenInfo>(AElfContractMethodName.GetTokenInfo, getTokenInfoParam,
            _chainOptions.ChainInfos[chainId].TokenContractAddress, chainId);
    }

    private async Task<AElfClient> GetAElfClientAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        return client;
    }
}