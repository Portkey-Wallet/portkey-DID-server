using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Monitor;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.Signature;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans;
using Portkey.Contracts.CA;
using Portkey.Contracts.RedPacket;
using Portkey.Contracts.TokenClaim;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetHolderInfoOutput> GetHolderInfoAsync(Hash caHash, Hash loginGuardianIdentifierHash, string chainId);
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
    public Task<GetBalanceOutput> GetBalanceAsync(string symbol, string address, string chainId);
    public Task ClaimTokenAsync(string symbol, string address, string chainId);

    public Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(
        GrainResultDto<RedPackageDetailDto> redPackageDetail, string payRedPackageFrom);
    
    public Task<TransactionInfoDto> SendTransferRedPacketRefundAsync(RedPackageDetailDto redPackageDetail,
        string payRedPackageFrom);

    public Task<SendTransactionOutput> SendTransferAsync(string symbol, string amount, string address, string chainId);
    Task<SendTransactionOutput> SendRawTransactionAsync(string chainId, string rawTransaction);
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);
    Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfo transactionInfo);

    Task<TransactionResultDto> SyncTransactionAsync(string chainId,
        SyncHolderInfoInput syncHolderInfoInput);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly ISignatureProvider _signatureProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IRedPackageAppService _redPackageAppService;
    private readonly GrainOptions _grainOptions;
    private readonly IIndicatorScope _indicatorScope;

    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        IClusterClient clusterClient,
        ISignatureProvider signatureProvider, IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption,
        IOptionsSnapshot<ContractOptions> contractOptions, IIndicatorScope indicatorScope,
        IRedPackageAppService redPackageAppService, IOptions<GrainOptions> grainOptions)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureProvider = signatureProvider;
        _redPackageAppService = redPackageAppService;
        _grainOptions = grainOptions.Value;
        _indicatorScope = indicatorScope;
        _contractOptions = contractOptions.Value;
        _clusterClient = clusterClient;
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result = await grain.SyncTransactionAsync(chainId, input);

            _logger.LogInformation(
                "SyncTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId, result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncTransaction to Chain: {id} Error: {input}", chainId,
                JsonConvert.SerializeObject(input.ToString(), Formatting.Indented));
            return new TransactionResultDto();
        }
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId,
        TransactionInfo transactionInfo)
    {
        try
        {
            if (transactionInfo == null)
            {
                return new SyncHolderInfoInput();
            }

            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var syncHolderInfoInput = await grain.GetSyncHolderInfoInputAsync(chainId, transactionInfo);

            _logger.LogInformation("GetSyncHolderInfoInput on chain {id} succeed", chainId);

            return syncHolderInfoInput;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncHolderInfoInput on chain: {id} error: {dto}", chainId,
                JsonConvert.SerializeObject(transactionInfo ?? new TransactionInfo(),
                    Formatting.Indented));
            return new SyncHolderInfoInput();
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

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(_contractOptions.CommonPrivateKeyForCallTx);

        var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.GenerateTransactionAsync.ToString());
        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);
        _indicatorScope.End(generateIndicator);

        _logger.LogDebug("Call tx methodName is: {methodName} param is: {transaction}", methodName, transaction);

        var txWithSign = client.SignTransaction(_contractOptions.CommonPrivateKeyForCallTx, transaction);

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

    public async Task<TransactionInfoDto> SendTransferRedPacketRefundAsync(RedPackageDetailDto redPackageDetail,
        string payRedPackageFrom)
    {
        var list = new List<TransferRedPacketInput>();

        Guid redPackageId = redPackageDetail.Id;
        string symbol = redPackageDetail.Symbol;
        string chainId = redPackageDetail.ChainId;
        var redPackageKeyGrain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageDetail.Id);
        var res = _redPackageAppService.GetRedPackageOption(redPackageDetail.Symbol,
            redPackageDetail.ChainId, out long maxCount,out string redPackageContractAddress);
        var grab = redPackageDetail.Items.Sum(item => long.Parse(item.Amount));
        list.Add(new TransferRedPacketInput
        {
            Amount = Convert.ToInt64((long.Parse(redPackageDetail.TotalAmount) - grab).ToString()),
            ReceiverAddress = Address.FromBase58(redPackageContractAddress),
            RedPacketSignature =await redPackageKeyGrain.GenerateSignature($"{symbol}-{redPackageDetail.MinAmount}-{maxCount}")
        });
        var sendInput = new TransferRedPacketBatchInput()
        {
            RedPacketId = redPackageId.ToString(),
            TransferRedPacketInputs = { list }
        };
        var contractServiceGrain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        return await contractServiceGrain.SendTransferRedPacketToChainAsync(chainId, sendInput, payRedPackageFrom,redPackageContractAddress);
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

    public async Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(
        GrainResultDto<RedPackageDetailDto> redPackageDetail, string payRedPackageFrom)
    {
        _logger.LogInformation("SendTransferRedPacketToChainAsync message: " + "\n{redPackageDetail}",
            JsonConvert.SerializeObject(redPackageDetail, Formatting.Indented));        
        //build param for transfer red package input 
        var list = new List<TransferRedPacketInput>();
        var redPackageId = redPackageDetail.Data.Id;
        var symbol = redPackageDetail.Data.Symbol;
        var chainId = redPackageDetail.Data.ChainId;

        var redPackageKeyGrain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageDetail.Data.Id);
        var res = _redPackageAppService.GetRedPackageOption(redPackageDetail.Data.Symbol,
            redPackageDetail.Data.ChainId, out var maxCount,out var redPackageContractAddress);
        _logger.LogInformation("GetRedPackageOption message: " + "\n{res}",
            JsonConvert.SerializeObject(res, Formatting.Indented)); 
        foreach (var item in redPackageDetail.Data.Items.Where(o => !o.PaymentCompleted).ToArray())
        {
           
            list.Add(new TransferRedPacketInput()
            {
                Amount = Convert.ToInt64(item.Amount),
                ReceiverAddress = Address.FromBase58(item.CaAddress),
                RedPacketSignature = await redPackageKeyGrain.GenerateSignature($"{symbol}-{res.MinAmount}-{maxCount}")
            });
        }

        var sendInput = new TransferRedPacketBatchInput()
        {
            RedPacketId = redPackageId.ToString(),
            TransferRedPacketInputs = { list }
        };
        _logger.LogInformation("SendTransferRedPacketToChainAsync sendInput: " + "\n{sendInput}",
            JsonConvert.SerializeObject(sendInput, Formatting.Indented)); 
        var contractServiceGrain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        return await contractServiceGrain.SendTransferRedPacketToChainAsync(chainId, sendInput, payRedPackageFrom,redPackageContractAddress);
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