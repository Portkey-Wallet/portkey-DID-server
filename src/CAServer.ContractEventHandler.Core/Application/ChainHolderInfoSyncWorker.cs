using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace CAServer.ContractEventHandler.Core.Application;

public abstract class ChainHolderInfoSyncWorker
{
    protected readonly IContractProvider _contractProvider;
    protected readonly ILogger<ContractAppService> _logger;
    private readonly IIndicatorLogger _indicatorLogger;

    protected ChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger, IIndicatorLogger indicatorLogger)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _indicatorLogger = indicatorLogger;
    }

    public abstract Task<bool> SyncAsync(string sideChainId, string targetChainId, long validateHeight, TransactionInfoDto transactionDto);
    public abstract Task ProcessSyncRecord(string chainId, string targetChainId, SyncRecord record, IndexOptions indexOptions);
    
    public abstract Task ProcessSyncRecordList(string chainId, string targetChainId, List<SyncRecord> recordList, IndexOptions indexOptions);

    protected async Task AddMonitorLogAsync(string fromChainId, long startHeight, string targetChainId, long endHeight,
        string changeType)
    {
        try
        {
            if (!_indicatorLogger.IsEnabled()) return;

            var startBlock = await _contractProvider.GetBlockByHeightAsync(fromChainId, startHeight);
            var endBlock = await _contractProvider.GetBlockByHeightAsync(targetChainId, endHeight);
            var blockInterval = endBlock.Header.Time - startBlock.Header.Time;
            var duration = (int)blockInterval.TotalMilliseconds;
            _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, changeType, duration);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add monitor log error.");
        }
    }
}

public class MainChainHolderInfoSyncWorker : ChainHolderInfoSyncWorker
{
    public MainChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger, IIndicatorLogger indicatorLogger) : base(contractProvider, logger, indicatorLogger)
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
        _ = AddMonitorLogAsync(chainId, record.BlockHeight, targetChainId, result.BlockNumber,
            record.ChangeType);
        _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
            record.ChangeType, chainId, record.CaHash);
    }
    
    public override async Task ProcessSyncRecordList(string chainId, string targetChainId, List<SyncRecord> records, IndexOptions indexOptions)
    {
        var tasks = records.Select(r =>
            _contractProvider.GetSyncHolderInfoInputAsync(chainId, r.ValidateTransactionInfoDto)).ToList();
        var syncHolderInfoInputList = await tasks.WhenAll();
        var resultList = await _contractProvider.SyncTransactionListAsync(targetChainId, syncHolderInfoInputList.ToList());
        //wait
        await _contractProvider.QueryTransactionResultAsync(chainId, resultList);
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
            _ = AddMonitorLogAsync(chainId, record.BlockHeight, targetChainId, result.BlockNumber,
                record.ChangeType);
            _logger.LogInformation("{type} SyncToSide succeed on chain: {id} of account: {hash}",
                record.ChangeType, chainId, record.CaHash);
        }
    }
}

public class SideChainHolderInfoSyncWorker : ChainHolderInfoSyncWorker
{
    public SideChainHolderInfoSyncWorker(IContractProvider contractProvider, ILogger<ContractAppService> logger, IIndicatorLogger indicatorLogger) : base(contractProvider, logger, indicatorLogger)
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
        _ = AddMonitorLogAsync(chainId, record.BlockHeight, ContractAppServiceConstant.MainChainId,
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

        
        var tasks = records.Select(r =>
            _contractProvider.GetSyncHolderInfoInputAsync(chainId, r.ValidateTransactionInfoDto)).ToList();
        var syncHolderInfoInputList = await tasks.WhenAll();
        var resultList = await _contractProvider.SyncTransactionListAsync(ContractAppServiceConstant.MainChainId, syncHolderInfoInputList.ToList());
        
        //wait
        await _contractProvider.QueryTransactionResultAsync(ContractAppServiceConstant.MainChainId, resultList);
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
            _ = AddMonitorLogAsync(chainId, record.BlockHeight, ContractAppServiceConstant.MainChainId,
                result.BlockNumber,
                record.ChangeType);
            _logger.LogInformation("{type} SyncToMain succeed on chain: {id} of account: {hash}",
                record.ChangeType, chainId, record.CaHash);
        }
    }
}