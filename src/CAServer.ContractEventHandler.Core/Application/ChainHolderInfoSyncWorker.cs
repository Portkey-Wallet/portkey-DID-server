using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Volo.Abp.Caching;

namespace CAServer.ContractEventHandler.Core.Application;

public abstract class ChainHolderInfoSyncWorker
{
    protected readonly IContractProvider _contractProvider;
    protected readonly ILogger<ContractAppService> _logger;
    protected readonly IMonitorLogProvider _monitorLogProvider;
    protected readonly IDistributedCache<string> _distributedCache;

    protected ChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger,
    IMonitorLogProvider monitorLogProvider, IDistributedCache<string> distributedCache)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _monitorLogProvider = monitorLogProvider;
        _distributedCache = distributedCache;
    }

    public abstract Task<bool> SyncAsync(string sideChainId, string targetChainId, long validateHeight, TransactionInfoDto transactionDto);
    public abstract Task ProcessSyncRecord(string chainId, string targetChainId, SyncRecord record, IndexOptions indexOptions);
    
    public abstract Task ProcessSyncRecordList(string chainId, string targetChainId, List<SyncRecord> recordList, IndexOptions indexOptions);

    protected async Task BeforeSyncRecordAsync(string targetChainId, SyncRecord record)
    {
        if (!await CheckSyncHolderVersionAsync(targetChainId, record.CaHash, record.ValidateHeight))
        {
            record.RecordStatus = RecordStatus.SKIPPED;
            return;
        }

        _monitorLogProvider.AddNode(record, DataSyncType.BeginSync);
    }

    private async Task<bool> CheckSyncHolderVersionAsync(string targetChainId, string caHash, long updateVersion)
    {
        var cacheKey = $"{ContractEventConstants.SyncHolderUpdateVersionCachePrefix}:{targetChainId}:{caHash}";
        var lastUpdateVersion = await _distributedCache.GetAsync(cacheKey);
        if (!lastUpdateVersion.IsNullOrWhiteSpace() && long.Parse(lastUpdateVersion) > updateVersion)
        {
            _logger.LogInformation("skip syncHolder targetChainId: {chainId}, caHash :{caHash},lastUpdateVersion:{version},curVersion:{curVersion}", 
                targetChainId, caHash, lastUpdateVersion, updateVersion);
            return false;
        }

        return true;
    }
    
    protected async Task UpdateSyncHolderVersionAsync(string targetChainId, string caHash, long updateVersion)
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
}

public class MainChainHolderInfoSyncWorker : ChainHolderInfoSyncWorker
{
    public MainChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger, 
        IMonitorLogProvider monitorLogProvider, IDistributedCache<string> distributedCache) : base(contractProvider, logger, monitorLogProvider, distributedCache)
    {
    }

    public override async Task<bool> SyncAsync(string sideChainId, string targetChainId, long validateHeight, TransactionInfoDto transactionDto)
    {
        await _contractProvider.SideChainCheckMainChainBlockIndexAsync(sideChainId, validateHeight);

        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(targetChainId, new TransactionInfo
            {
                TransactionId = transactionDto.TransactionResultDto.TransactionId,
                BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                Transaction = transactionDto.Transaction.ToByteArray()
            });

        if (syncHolderInfoInput.VerificationTransactionInfo == null)
        {
            return false;
        }

        var resultDto = await _contractProvider.SyncTransactionAsync(sideChainId, syncHolderInfoInput);
        return resultDto.Status == TransactionState.Mined;
    }

    public override async Task ProcessSyncRecord(string chainId, string targetChainId, SyncRecord record, IndexOptions indexOptions)
    {
        var syncHolderInfoInput = await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
        var result = await _contractProvider.SyncTransactionAsync(targetChainId, syncHolderInfoInput);
        record.RecordStatus = RecordStatus.SYNCED;
        if (result.Status != TransactionState.Mined)
        {
            _logger.LogError(
                "{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}",
                record.ChangeType, chainId, record.CaHash, result.Error);

            record.RetryTimes++;
            record.ValidateHeight = long.MaxValue;
            record.ValidateTransactionInfoDto = new TransactionInfo();
            record.RecordStatus = RecordStatus.NOT_MINED;
            return;
        }

        record.RecordStatus = RecordStatus.MINED;
        // do not wait
        _ = _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, targetChainId, result.BlockNumber,
            record.ChangeType);
        _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
            record.ChangeType, chainId, record.CaHash);
    }

    public override async Task ProcessSyncRecordList(string chainId, string targetChainId, List<SyncRecord> records, IndexOptions indexOptions)
    {
        var beforeSyncTasks = records.Select(r => BeforeSyncRecordAsync(targetChainId, r));
        await beforeSyncTasks.WhenAll();
        records = records.Where(r => r.RecordStatus != RecordStatus.SKIPPED).ToList();
        
        var tasks = records.Select(r =>
            _contractProvider.GetSyncHolderInfoInputAsync(chainId, r.ValidateTransactionInfoDto)).ToList();
        var syncHolderInfoInputList = await tasks.WhenAll();
        var resultList = await _contractProvider.SyncTransactionListAsync(targetChainId, syncHolderInfoInputList.ToList());
        await _contractProvider.QueryTransactionResultAsync(targetChainId, resultList, true);
        for (int i = 0; i < resultList.Count; i++)
        {
            // check sync version
            var result = resultList[i];
            var record = records[i];
            if (record == null)
            {
                continue;
            }
            record.RecordStatus = RecordStatus.SYNCED;
            if (result.Status != TransactionState.Mined)
            {
                _logger.LogError(
                    "{type} SyncToSide failed on chain: {id} of account: {hash}, error: {error}",
                    record.ChangeType, chainId, record.CaHash, result.Error);

                record.RetryTimes++;
                record.ValidateHeight = long.MaxValue;
                record.ValidateTransactionInfoDto = new TransactionInfo();
                record.RecordStatus = RecordStatus.NOT_MINED;
                continue;
            }

            record.RecordStatus = RecordStatus.MINED;
            // do not wait
            _ = UpdateSyncHolderVersionAsync(targetChainId, record.CaHash, record.ValidateHeight);
            _ = _monitorLogProvider.FinishAsync(record, targetChainId, result.BlockNumber);
            _ = _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, targetChainId,
                result.BlockNumber,
                record.ChangeType);
            _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                record.ChangeType, chainId, record.CaHash);
        }
    }
}

public class SideChainHolderInfoSyncWorker : ChainHolderInfoSyncWorker
{
    public SideChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger, 
        IMonitorLogProvider monitorLogProvider, IDistributedCache<string> distributedCache) : base(contractProvider, logger, monitorLogProvider, distributedCache)
    {
    }

    public override async Task<bool> SyncAsync(string sideChainId, string targetChainId, long validateHeight, TransactionInfoDto transactionDto)
    {
        await _contractProvider.MainChainCheckSideChainBlockIndexAsync(sideChainId, validateHeight);

        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(sideChainId, new TransactionInfo
            {
                TransactionId = transactionDto.TransactionResultDto.TransactionId,
                BlockNumber = transactionDto.TransactionResultDto.BlockNumber,
                Transaction = transactionDto.Transaction.ToByteArray()
            });

        if (syncHolderInfoInput.VerificationTransactionInfo == null)
        {
            return false;
        }

        var resultDto =
            await _contractProvider.SyncTransactionAsync(targetChainId, syncHolderInfoInput);
        return resultDto.Status == TransactionState.Mined;
    }

    public override async Task ProcessSyncRecord(string chainId, string targetChainId, SyncRecord record, IndexOptions indexOptions)
    {
        var retryTimes = 0;
        var mainHeight = await _contractProvider.GetBlockHeightAsync(targetChainId);
        var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);

        while (indexMainChainBlock <= mainHeight && retryTimes < indexOptions.IndexTimes)
        {
            await Task.Delay(indexOptions.IndexDelay);
            indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);
            retryTimes++;
        }

        var syncHolderInfoInput =
            await _contractProvider.GetSyncHolderInfoInputAsync(chainId, record.ValidateTransactionInfoDto);
        var result =
            await _contractProvider.SyncTransactionAsync(ContractAppServiceConstant.MainChainId,
                syncHolderInfoInput);
        record.RecordStatus = RecordStatus.SYNCED;

        if (result.Status != TransactionState.Mined)
        {
            _logger.LogError("{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}",
                record.ChangeType, chainId, record.CaHash, result.Error);

            record.RetryTimes++;
            record.ValidateHeight = long.MaxValue;
            record.ValidateTransactionInfoDto = new TransactionInfo();
            record.RecordStatus = RecordStatus.NOT_MINED;
            return;
        }

        record.RecordStatus = RecordStatus.MINED;

        // do not wait
        _ = _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, ContractAppServiceConstant.MainChainId,
            result.BlockNumber,
            record.ChangeType);
        _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
            record.ChangeType, chainId, record.CaHash);
    }

    public override async Task ProcessSyncRecordList(string chainId, string targetChainId, List<SyncRecord> records, IndexOptions indexOptions)
    {
        var retryTimes = 0;
        var mainHeight = await _contractProvider.GetBlockHeightAsync(targetChainId);
        var indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);

        while (indexMainChainBlock <= mainHeight && retryTimes < indexOptions.IndexTimes)
        {
            await Task.Delay(indexOptions.IndexDelay);
            indexMainChainBlock = await _contractProvider.GetIndexHeightFromSideChainAsync(chainId);
            retryTimes++;
        }
        
        var beforeSyncTasks = records.Select(r => BeforeSyncRecordAsync(targetChainId, r));
        await beforeSyncTasks.WhenAll();
        records = records.Where(r => r.RecordStatus != RecordStatus.SKIPPED).ToList();
        
        var tasks = records.Select(r =>
            _contractProvider.GetSyncHolderInfoInputAsync(chainId, r.ValidateTransactionInfoDto)).ToList();
        var syncHolderInfoInputList = await tasks.WhenAll();
        var resultList = await _contractProvider.SyncTransactionListAsync(targetChainId, syncHolderInfoInputList.ToList());
        
        //wait
        await _contractProvider.QueryTransactionResultAsync(targetChainId, resultList, true);
        for (int i = 0; i < resultList.Count; i++)
        {
            var result = resultList[i];
            var record = records[i];
            if (record == null)
            {
                continue;
            }
            record.RecordStatus = RecordStatus.SYNCED;

            if (result.Status != TransactionState.Mined)
            {
                _logger.LogError("{type} SyncToMain failed on chain: {id} of account: {hash}, error: {error}",
                    record.ChangeType, chainId, record.CaHash, result.Error);

                record.RetryTimes++;
                record.ValidateHeight = long.MaxValue;
                record.ValidateTransactionInfoDto = new TransactionInfo();
                record.RecordStatus = RecordStatus.NOT_MINED;
                return;
            }

            record.RecordStatus = RecordStatus.MINED;

            // do not wait
            _ = UpdateSyncHolderVersionAsync(targetChainId, record.CaHash, record.ValidateHeight);
            _ = _monitorLogProvider.FinishAsync(record, targetChainId, result.BlockNumber);
            _ = _monitorLogProvider.AddMonitorLogAsync(chainId, record.BlockHeight, targetChainId,
                result.BlockNumber,
                record.ChangeType);
            _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                record.ChangeType, chainId, record.CaHash);
        }
    }
}