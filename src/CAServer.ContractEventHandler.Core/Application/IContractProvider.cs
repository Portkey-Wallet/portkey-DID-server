using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Grains.Grain;
using CAServer.Commons;
using CAServer.ContractService;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.RedPackage.Dtos;
using CAServer.Monitor;
using CAServer.Signature.Provider;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Portkey.Contracts.CA;
using Portkey.Contracts.CryptoBox;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractProvider : ISingletonDependency
{
    Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId,
        Hash loginGuardian, string caHash);

    Task<int> GetChainIdAsync(string chainId);
    Task<long> GetBlockHeightAsync(string chainId);
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId);
    Task<long> GetIndexHeightFromSideChainAsync(string sideChainId);
    Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId);
    Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);

    Task<TransactionResultDto> CreateHolderInfoOnNonCreateChainAsync(ChainInfo chainInfo,
        GetHolderInfoOutput outputGetHolderInfo,
        CreateHolderDto createHolderDto);

    Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);

    Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardians);

    Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfo transactionInfo);

    Task<TransactionResultDto> SyncTransactionAsync(string chainId,
        SyncHolderInfoInput syncHolderInfoInput);

    Task MainChainCheckSideChainBlockIndexAsync(string chainIdSide, long txHeight);
    Task SideChainCheckMainChainBlockIndexAsync(string sideChainId, long txHeight);

    Task<ChainStatusDto> GetChainStatusAsync(string chainId);
    Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false);
    Task<TransactionResultDto> ForwardTransactionAsync(string chainId, string rawTransaction);

    Task<TransactionInfoDto> SendTransferRedPacketRefundAsync(RedPackageDetailDto redPackageDetail,
        string payRedPackageFrom);

    public Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(
        GrainResultDto<RedPackageDetailDto> redPackageDetail, string payRedPackageFrom);

    Task<TransactionResultDto> AppendGuardianPoseidonHashAsync(string chainId, AppendGuardianRequest appendGuardianRequest);
    
    Task<TransactionResultDto> AppendSingleGuardianPoseidonAsync(string chainId, GuardianIdentifierType guardianIdentifierType, AppendSingleGuardianPoseidonInput input);
}

public class ContractProvider : IContractProvider
{
    private readonly ILogger<ContractProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ChainOptions _chainOptions;
    private readonly IndexOptions _indexOptions;
    private readonly ISignatureProvider _signatureProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IIndicatorScope _indicatorScope;
    private readonly IDistributedCache<BlockDto> _distributedCache;
    private readonly BlockInfoOptions _blockInfoOptions;
    private readonly ContractServiceProxy _contractServiceProxy;


    public ContractProvider(ILogger<ContractProvider> logger, IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<IndexOptions> indexOptions, IClusterClient clusterClient, ISignatureProvider signatureProvider,
        IGraphQLProvider graphQlProvider, IIndicatorScope indicatorScope, IDistributedCache<BlockDto> distributedCache,
        IOptionsSnapshot<BlockInfoOptions> blockInfoOptions, ContractServiceProxy contractServiceProxy)
    {
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _indexOptions = indexOptions.Value;
        _clusterClient = clusterClient;
        _signatureProvider = signatureProvider;
        _graphQlProvider = graphQlProvider;
        _indicatorScope = indicatorScope;
        _distributedCache = distributedCache;
        _contractServiceProxy = contractServiceProxy;
        _blockInfoOptions = blockInfoOptions.Value;
    }

    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain) where T : IMessage<T>, new()
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
            var contractAddress = isCrossChain ? chainInfo.CrossChainContractAddress : chainInfo.ContractAddress;

            var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                MonitorAelfClientType.GenerateTransactionAsync.ToString());
            var transaction =
                await client.GenerateTransactionAsync(ownAddress, contractAddress,
                    methodName, param);
            _indicatorScope.End(generateIndicator);

            var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());
            transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

            var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                MonitorAelfClientType.ExecuteTransactionAsync.ToString());

            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            });

            _indicatorScope.End(interIndicator);
            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

            return value;
        }
        catch (Exception e)
        {
            if (methodName != MethodName.GetHolderInfo)
            {
                _logger.LogError(e, methodName + " error: {param}", param);
            }

            return new T();
        }
    }

    public async Task<TransactionResultDto> ForwardTransactionAsync(string chainId, string rawTransaction)
    {
        try
        {
            var result = await _contractServiceProxy.ForwardTransactionAsync(chainId, rawTransaction);

            _logger.LogInformation(
                "ForwardTransactionAsync to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ForwardTransactionAsync error, chainId {chainId}",
                JsonConvert.SerializeObject(chainId, Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId, Hash loginGuardian,
        string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianIdentifierHash = loginGuardian
        };

        if (caHash != null)
        {
            param.CaHash = Hash.LoadFromHex(caHash);
        }

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, MethodName.GetHolderInfo, param, false);

        if (output == null || output.CaHash.IsNullOrEmpty())
        {
            _logger.LogInformation(MethodName.GetHolderInfo + ": Empty result");
            return new GetHolderInfoOutput();
        }

        // _logger.LogDebug(MethodName.GetHolderInfo + " result: {output}",
        //     JsonConvert.SerializeObject(output.ToString(), Formatting.Indented));

        return output;
    }

    public async Task<int> GetChainIdAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetChainIdAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetChainId on chain: {id} error", chainId);
            return ContractAppServiceConstant.IntError;
        }
    }

    public async Task<long> GetBlockHeightAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetBlockHeightAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBlockHeight on chain: {id} error", chainId);
            return ContractAppServiceConstant.LongError;
        }
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            return await client.GetTransactionResultAsync(txId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransactionResult on chain {chainId} with txId: {txId} error", chainId, txId);
            return new TransactionResultDto();
        }
    }

    public async Task<long> GetIndexHeightFromSideChainAsync(string sideChainId)
    {
        var result =
            await CallTransactionAsync<Int64Value>(sideChainId, MethodName.GetParentChainHeight, new Empty(), true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogError(MethodName.GetParentChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId)
    {
        var param = new Int32Value
        {
            Value = sideChainId
        };

        var result = await CallTransactionAsync<Int64Value>(chainId, MethodName.GetSideChainHeight, param, true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogError(MethodName.GetSideChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        try
        {
            var result = await _contractServiceProxy.CreateHolderInfoAsync(createHolderDto);

            _logger.LogInformation(
                "CreateHolderInfo to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                createHolderDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateHolderInfo error: {message}",
                JsonConvert.SerializeObject(createHolderDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionResultDto> CreateHolderInfoOnNonCreateChainAsync(ChainInfo chainInfo,
        GetHolderInfoOutput outputGetHolderInfo,
        CreateHolderDto createHolderDto)
    {
        try
        {
            if (outputGetHolderInfo == null || outputGetHolderInfo.CaHash == null ||
                outputGetHolderInfo.CaHash.Value.IsNullOrEmpty())
            {
                _logger.LogInformation("cannot execute accelerated registration, 'CaHash' is null");
                return new TransactionResultDto
                {
                    Status = TransactionState.Failed,
                    Error = "cannot execute accelerated registration, 'CaHash' is null"
                };
            }

            var createChainId = ChainHelper.ConvertChainIdToBase58(outputGetHolderInfo.CreateChainId);
            if (createChainId == chainInfo.ChainId)
            {
                _logger.LogInformation("cannot execute accelerated registration on the Create Chain, {0}",
                    createHolderDto?.GuardianInfo?.IdentifierHash);
                return new TransactionResultDto
                {
                    Status = TransactionState.Failed,
                    Error = "cannot execute accelerated registration on the Create Chain"
                };
            }

            createHolderDto.CaHash = outputGetHolderInfo.CaHash;
            createHolderDto.ChainId = createChainId;

            var result = await _contractServiceProxy.CreateHolderInfoOnNonCreateChainAsync(chainInfo.ChainId, createHolderDto);

            _logger.LogInformation(
                "accelerated registration on chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                createHolderDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "accelerated registration error: {chainId}, {message}", chainInfo.ChainId,
                JsonConvert.SerializeObject(createHolderDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = $"accelerated registration error:{e.Message}"
            };
        }
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        try
        {
            var result = await _contractServiceProxy.SocialRecoveryAsync(socialRecoveryDto);
            if (result != null)
            {
                _logger.LogInformation(
                                "SocialRecovery to chain: {id} result:" +
                                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                                socialRecoveryDto.ChainId,
                                result.TransactionId, result.BlockNumber, result.Status, result.Error);
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SocialRecovery error: {message}",
                JsonConvert.SerializeObject(socialRecoveryDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardians)
    {
        try
        {
            await CheckCreateChainIdAsync(result);
            
            var transactionDto = await _contractServiceProxy.ValidateTransactionAsync(chainId, result, unsetLoginGuardians);

            _logger.LogInformation(
                "ValidateTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId,
                transactionDto.TransactionResultDto.TransactionId, transactionDto.TransactionResultDto.BlockNumber,
                transactionDto.TransactionResultDto.Status,
                transactionDto.TransactionResultDto.Error);

            return transactionDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateTransaction on chain: {id} error", chainId);
            return new TransactionInfoDto();
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

            var syncHolderInfoInput = await _contractServiceProxy.GetSyncHolderInfoInputAsync(chainId, transactionInfo);

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

    public async Task MainChainCheckSideChainBlockIndexAsync(string chainIdSide, long txHeight)
    {
        var mainHeight = long.MaxValue;
        var checkResult = false;
        var sideChainIdInInt = await GetChainIdAsync(chainIdSide);
        var time = 0;

        while (!checkResult && time < _indexOptions.IndexTimes)
        {
            var indexSideChainBlock =
                await GetIndexHeightFromMainChainAsync(ContractAppServiceConstant.MainChainId,
                    sideChainIdInInt);

            if (indexSideChainBlock < txHeight)
            {
                _logger.LogInformation(LoggerMsg.IndexBlockRecordInformation);
                await Task.Delay(_indexOptions.IndexDelay);
                time++;
                continue;
            }

            mainHeight = mainHeight == long.MaxValue
                ? await GetBlockHeightAsync(ContractAppServiceConstant.MainChainId)
                : mainHeight;

            var indexMainChainBlock = await GetIndexHeightFromSideChainAsync(chainIdSide);
            checkResult = indexMainChainBlock > mainHeight;
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    public async Task SideChainCheckMainChainBlockIndexAsync(string sideChainId, long txHeight)
    {
        var checkResult = false;
        var time = 0;

        while (!checkResult && time < _indexOptions.IndexTimes)
        {
            var indexMainChainBlock = await GetIndexHeightFromSideChainAsync(sideChainId);

            if (indexMainChainBlock < txHeight)
            {
                _logger.LogInformation(LoggerMsg.IndexBlockRecordInformation);
                await Task.Delay(_indexOptions.IndexDelay);
                time++;
                continue;
            }

            checkResult = true;
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    private void CheckIndexBlockHeightResult(bool result, int time)
    {
        if (!result && time == _indexOptions.IndexTimes)
        {
            _logger.LogError(LoggerMsg.IndexTimeoutError);
            throw new UserFriendlyException(LoggerMsg.IndexTimeoutError);
        }
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        try
        {
            var result = await _contractServiceProxy.SyncTransactionAsync(chainId, input);

            _logger.LogInformation(
                "SyncTransaction to chain: {id} result:" +
                "TransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
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

    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);

        var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
            MonitorAelfClientType.GetChainStatusAsync.ToString());

        var chainStatusDto = await client.GetChainStatusAsync();
        _indicatorScope.End(interIndicator);

        return chainStatusDto;
    }

    public async Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
    {
        var cacheKey = $"{ContractEventConstants.BlockHeightCachePrefix}:{chainId}:{height}";
        var blockInfo = await _distributedCache.GetOrAddAsync(cacheKey, async () =>
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];
            var client = new AElfClient(chainInfo.BaseUrl);
            return await client.GetBlockByHeightAsync(height, includeTransactions);
        }, () => new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_blockInfoOptions.BlockCacheExpire)
        });

        return blockInfo;
    }

    private async Task CheckCreateChainIdAsync(GetHolderInfoOutput holderInfoOutput)
    {
        if (holderInfoOutput.CreateChainId > 0) return;

        var holderInfos = await _graphQlProvider.GetCaHolderInfoAsync(holderInfoOutput.CaHash.ToHex());
        var holderInfo = holderInfos?.CaHolderInfo?.FirstOrDefault();
        if (holderInfo == null) return;

        holderInfoOutput.CreateChainId = ChainHelper.ConvertBase58ToChainId(holderInfo.OriginChainId);
    }

    public async Task<TransactionInfoDto> SendTransferRedPacketRefundAsync(RedPackageDetailDto redPackageDetail,
        string payRedPackageFrom)
    {
        var redPackageId = redPackageDetail.Id;
        var chainId = redPackageDetail.ChainId;
        var redPackageKeyGrain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageDetail.Id);
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }
        var grab = redPackageDetail.Items.Sum(item => long.Parse(item.Amount));
        var sendInput = new RefundCryptoBoxInput
        {
            CryptoBoxId = redPackageId.ToString(),
            Amount = long.Parse(redPackageDetail.TotalAmount) - grab,
            CryptoBoxSignature =
                await redPackageKeyGrain.GenerateSignature(
                    $"{redPackageId}-{long.Parse(redPackageDetail.TotalAmount) - grab}")
        };
        _logger.LogInformation("SendTransferRedPacketRefundAsync input {input}",JsonConvert.SerializeObject(sendInput));
        
        return await _contractServiceProxy.SendTransferRedPacketToChainAsync(chainId, sendInput, payRedPackageFrom,
            chainInfo.RedPackageContractAddress, MethodName.RefundCryptoBox);
    }


    public async Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(
        GrainResultDto<RedPackageDetailDto> redPackageDetail, string payRedPackageFrom)
    {
        _logger.LogInformation("SendTransferRedPacketToChainAsync message: " + "\n{redPackageDetail}",
            JsonConvert.SerializeObject(redPackageDetail, Formatting.Indented));
        //build param for transfer red package input 
        var list = new List<TransferCryptoBoxInput>();
        var redPackageId = redPackageDetail.Data.Id;
        var chainId = redPackageDetail.Data.ChainId;
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var redPackageKeyGrain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageDetail.Data.Id);
        _logger.Debug("SendTransferRedPacketToChainAsync message: {redPackageId}", redPackageDetail.Data.Id.ToString());
        foreach (var item in redPackageDetail.Data.Items.Where(o => !o.PaymentCompleted).ToArray())
        {
            _logger.LogInformation("redPackageKeyGrain GenerateSignature input{param}",
                $"{redPackageId}-{Address.FromBase58(item.CaAddress)}-{item.Amount}");
            list.Add(new TransferCryptoBoxInput()
            {
                Amount = Convert.ToInt64(item.Amount),
                Receiver = Address.FromBase58(item.CaAddress),
                CryptoBoxSignature =
                    await redPackageKeyGrain.GenerateSignature(
                        $"{redPackageId}-{Address.FromBase58(item.CaAddress)}-{item.Amount}")
            });
        }

        var sendInput = new TransferCryptoBoxesInput()
        {
            CryptoBoxId = redPackageId.ToString(),
            TransferCryptoBoxInputs = { list }
        };
        _logger.LogInformation("SendTransferRedPacketToChainAsync sendInput: " + "\n{sendInput}",
            JsonConvert.SerializeObject(sendInput, Formatting.Indented));
        var contractServiceGrain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        return await _contractServiceProxy.SendTransferRedPacketToChainAsync(chainId, sendInput, payRedPackageFrom,
            chainInfo.RedPackageContractAddress, MethodName.TransferCryptoBoxes);
    }
    
    public async Task<TransactionResultDto> AppendGuardianPoseidonHashAsync(string chainId, AppendGuardianRequest appendGuardianRequest)
    {
        try
        {
            var result = await _contractServiceProxy.AppendGuardianPoseidonHashAsync(chainId, appendGuardianRequest);

            _logger.LogInformation(
                "AppendGuardianPoseidonHash to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId, result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AppendGuardianPoseidonHash error: {message}",
                JsonConvert.SerializeObject(appendGuardianRequest, Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionResultDto> AppendSingleGuardianPoseidonAsync(string chainId, GuardianIdentifierType guardianIdentifierType, AppendSingleGuardianPoseidonInput input)
    {
        try
        {
            var result = await _contractServiceProxy.AppendSingleGuardianPoseidonAsync(chainId, guardianIdentifierType, input);

            _logger.LogInformation(
                "AppendSingleGuardianPoseidonHash to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId, result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AppendSingleGuardianPoseidonHash error: {message}",
                JsonConvert.SerializeObject(input, Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }
}