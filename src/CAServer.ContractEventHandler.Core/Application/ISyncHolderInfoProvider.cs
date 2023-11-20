using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface ISyncHolderInfoProvider
{
    ConcurrentDictionary<string, ConcurrentQueue<SyncRecord>> RecordDic { get; set; }
    Task<List<SyncRecord>> SyncHolderInfoAsync();
}

public class SyncHolderInfoProvider : ISyncHolderInfoProvider, ISingletonDependency
{
    public ConcurrentDictionary<string, ConcurrentQueue<SyncRecord>> RecordDic { get; set; } = new();
    private readonly IMonitorLogProvider _monitorLogProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<SyncHolderInfoProvider> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IndexOptions _indexOptions;
    private readonly ContractSyncOptions _contractSyncOptions;

    public SyncHolderInfoProvider(IOptionsSnapshot<ChainOptions> chainOptions, IMonitorLogProvider monitorLogProvider,
        IContractProvider contractProvider, ILogger<SyncHolderInfoProvider> logger,
        IDistributedCache<string> distributedCache,
        IOptionsSnapshot<IndexOptions> indexOptions,
        IOptions<ContractSyncOptions> contractSyncOptions)
    {
        _monitorLogProvider = monitorLogProvider;
        _contractProvider = contractProvider;
        _logger = logger;
        _distributedCache = distributedCache;
        _chainOptions = chainOptions.Value;
        _indexOptions = indexOptions.Value;
        _contractSyncOptions = contractSyncOptions.Value;

        foreach (var info in _chainOptions.ChainInfos)
        {
            RecordDic.TryAdd(info.Key, new ConcurrentQueue<SyncRecord>());
        }
    }

    public async Task<List<SyncRecord>> SyncHolderInfoAsync()
    {
        try
        {
            var tasks = new List<Task<List<SyncRecord>>>();
            foreach (var recordKeyPair in RecordDic)
            {
                if (recordKeyPair.Value.IsEmpty)
                {
                    continue;
                }

                var chainId = recordKeyPair.Key;
                var syncRecords = GetRecords(recordKeyPair.Value);
                _logger.LogInformation("sync records chainId: {chainId} count: {count}", chainId, syncRecords.Count);

                var records = new List<SyncRecord>();
                foreach (var record in syncRecords)
                {
                    if (records.Count < _contractSyncOptions.SyncOnceCount)
                    {
                        records.Add(record);
                        continue;
                    }

                    tasks.Add(SyncHolderInfoAsync(new List<SyncRecord>(records), chainId));
                    records.Clear();
                }

                if (!records.IsNullOrEmpty())
                {
                    tasks.Add(SyncHolderInfoAsync(new List<SyncRecord>(records), chainId));
                    records.Clear();
                }
            }

            var failedRecords = new List<SyncRecord>();
            var syncFailedRecords = await Task.WhenAll(tasks);

            foreach (var items in syncFailedRecords)
            {
                failedRecords.AddRange(items);
            }

            return failedRecords;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncHolderInfoAsync error");
            return new List<SyncRecord>();
        }
    }

    private List<SyncRecord> GetRecords(ConcurrentQueue<SyncRecord> queue)
    {
        var records = new List<SyncRecord>();
        while (!queue.IsEmpty)
        {
            var dequeue = queue.TryDequeue(out var record);
            if (dequeue)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private async Task<List<SyncRecord>> SyncHolderInfoAsync(List<SyncRecord> records, string chainId)
    {
        var failedRecords = new List<SyncRecord>();
        if (records.IsNullOrEmpty())
        {
            return failedRecords;
        }

        if (chainId == ContractAppServiceConstant.MainChainId)
        {
            failedRecords = await SyncMainChainAsync(records, chainId);
        }
        else
        {
            failedRecords = await SyncSideChainAsync(records, chainId);
        }

        return failedRecords;
    }

    private async Task<List<SyncRecord>> SyncMainChainAsync(List<SyncRecord> records, string chainId)
    {
        var failedRecords = new List<SyncRecord>();

        try
        {
            var syncHolderInfosInput = new SyncHolderInfosInput()
            {
                VerificationTransactionInfos = { }
            };

            var tasks = new List<Task<SyncHolderInfoInput>>();
            foreach (var record in records)
            {
                tasks.Add(GetSyncHolderInfoInputAsync(record, chainId));
            }

            var syncHolderInfoInputs = await Task.WhenAll(tasks);

            foreach (var syncHolderInput in syncHolderInfoInputs)
            {
                if (syncHolderInput == null)
                {
                    _logger.LogError("sync holder info, why valid method return null.");
                    continue;
                }

                syncHolderInfosInput.VerificationTransactionInfos.Add(syncHolderInput.VerificationTransactionInfo);
            }

            failedRecords = await SyncMainChainAsync(records, chainId, syncHolderInfosInput);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncMainChainAsync error");
        }

        return failedRecords;
    }

    private async Task<List<SyncRecord>> SyncMainChainAsync(List<SyncRecord> records, string chainId,
        SyncHolderInfosInput syncHolderInfosInput)
    {
        var failedRecords = new List<SyncRecord>();

        var chainInfo = _chainOptions.ChainInfos.Values.First(info => !info.IsMainChain);
        var result = await _contractProvider.SyncTransactionAsync(chainInfo.ChainId, syncHolderInfosInput);

        foreach (var record in records)
        {
            if (result.Status != TransactionState.Mined)
            {
                _logger.LogError(
                    "{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}, data:{data}",
                    record.ChangeType, chainId, record.CaHash, result.Error,
                    JsonConvert.SerializeObject(record.ValidateTransactionInfoDto));

                record.RetryTimes++;
                record.ValidateHeight = long.MaxValue;
                record.ValidateTransactionInfoDto = new TransactionInfo();

                failedRecords.Add(record);
            }
            else
            {
                await _monitorLogProvider.FinishAsync(record, chainInfo.ChainId, result.BlockNumber);
                await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, chainInfo.ChainId,
                    result.BlockNumber,
                    record.ChangeType);
                _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                    record.ChangeType, chainId, record.CaHash);
                await UpdateSyncHolderVersionAsync(chainInfo.ChainId, record.CaHash, record.ValidateHeight);
            }
        }

        return failedRecords;
    }

    private async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(SyncRecord record, string chainId)
    {
        _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);
        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(chainId,
                record.ValidateTransactionInfoDto);

        return syncHolderInfoInput;
    }

    private async Task UpdateSyncHolderVersionAsync(string targetChainId, string caHash, long updateVersion)
    {
        var cacheKey = $"{ContractEventConstants.SyncHolderUpdateVersionCachePrefix}:{targetChainId}:{caHash}";
        var lastUpdateVersion = await _distributedCache.GetAsync(cacheKey);
        if (lastUpdateVersion.IsNullOrWhiteSpace() || long.Parse(lastUpdateVersion) < updateVersion)
        {
            await _distributedCache.SetAsync(cacheKey, updateVersion.ToString(), new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(ContractEventConstants.SyncHolderUpdateVersionCacheExpireTime)
            });
        }
    }


    private async Task<List<SyncRecord>> SyncSideChainAsync(List<SyncRecord> records, string chainId)
    {
        var failedRecords = new List<SyncRecord>();
        try
        {
            var syncHolderInfosInput = new SyncHolderInfosInput()
            {
                VerificationTransactionInfos = { }
            };

            var tasks = new List<Task<SyncHolderInfoInput>>();
            foreach (var record in records)
            {
                tasks.Add(GetSideChainSyncHolderInfoInputAsync(record, chainId));
            }

            var syncHolderInfoInputs = await Task.WhenAll(tasks);
            foreach (var syncHolderInput in syncHolderInfoInputs)
            {
                if (syncHolderInput == null)
                {
                    _logger.LogError("sync holder info, why valid method return null.");
                    continue;
                }

                syncHolderInfosInput.VerificationTransactionInfos.Add(syncHolderInput.VerificationTransactionInfo);
            }

            failedRecords = await SyncSideChainAsync(records, chainId, syncHolderInfosInput);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncSideChainAsync error");
        }

        return failedRecords;
    }

    private async Task<SyncHolderInfoInput> GetSideChainSyncHolderInfoInputAsync(SyncRecord record, string chainId)
    {
        _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);

        var retryTimes = 0;
        var mainHeight =
            await _contractProvider.GetBlockHeightAsync(ContractAppServiceConstant.MainChainId);
        var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);

        while (indexMainChainBlock <= mainHeight && retryTimes < _indexOptions.IndexTimes)
        {
            await Task.Delay(_indexOptions.IndexDelay);
            indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);
            retryTimes++;
        }

        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
        return syncHolderInfoInput;
    }

    private async Task<List<SyncRecord>> SyncSideChainAsync(List<SyncRecord> records, string chainId,
        SyncHolderInfosInput syncHolderInfosInput)
    {
        var failedRecords = new List<SyncRecord>();

        var result =
            await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId, syncHolderInfosInput);

        foreach (var record in records)
        {
            if (result.Status != TransactionState.Mined)
            {
                _logger.LogError(
                    "{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}, data: {data}",
                    record.ChangeType, chainId, record.CaHash, result.Error,
                    JsonConvert.SerializeObject(record.ValidateTransactionInfoDto));

                record.RetryTimes++;
                record.ValidateHeight = long.MaxValue;
                record.ValidateTransactionInfoDto = new TransactionInfo();

                failedRecords.Add(record);
            }
            else
            {
                await _monitorLogProvider.FinishAsync(record, ContractAppServiceConstant.MainChainId,
                    result.BlockNumber);
                await _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight,
                    ContractAppServiceConstant.MainChainId,
                    result.BlockNumber,
                    record.ChangeType);
                await UpdateSyncHolderVersionAsync(ContractAppServiceConstant.MainChainId, record.CaHash,
                    record.ValidateHeight);
                _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                    record.ChangeType, chainId, record.CaHash);
            }
        }

        return failedRecords;
    }
}