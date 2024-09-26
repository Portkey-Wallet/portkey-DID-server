using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IMonitorLogProvider
{
    void AddNode(SyncRecord record, DataSyncType syncType);
    Task InitDataSyncMonitorAsync(List<SyncRecord> syncRecords, string chainId);
    Task FinishAsync(SyncRecord record, string endChainId, long endHeight);

    Task AddMonitorLogAsync(string startChainId, long startHeight, string endChainId, long endHeight,
        string changeType);

    Task AddHeightIndexMonitorLogAsync(string chainId, long indexHeight);
}

public class MonitorLogProvider : IMonitorLogProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<MonitorLogProvider> _logger;
    private readonly IIndicatorLogger _indicatorLogger;
    private readonly int _maxDuration = 300_000;

    public MonitorLogProvider(IContractProvider contractProvider, ILogger<MonitorLogProvider> logger,
        IIndicatorLogger indicatorLogger)
    {
        _contractProvider = contractProvider;
        _logger = logger;
        _indicatorLogger = indicatorLogger;
    }

    public async Task InitDataSyncMonitorAsync(List<SyncRecord> syncRecords, string chainId)
    {
        if (syncRecords.IsNullOrEmpty()) return;

        foreach (var syncRecord in syncRecords.Where(syncRecord => syncRecord.DataSyncMonitor == null))
        {
            try
            {
                await InitDataSyncMonitorAsync(syncRecord, chainId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "init data sync monitor log error, caHash: {caHash}, changeType: {changeType}",
                    syncRecord.CaHash, syncRecord.ChangeType);
            }
        }
    }

    private async Task InitDataSyncMonitorAsync(SyncRecord syncRecord, string chainId)
    {
        if (!_indicatorLogger.IsEnabled()) return;

        var block = await _contractProvider.GetBlockByHeightAsync(chainId, syncRecord.BlockHeight);

        var blockTime = TimeHelper.GetTimeStampFromDateTime(block.Header.Time);
        var getRecordTime = TimeHelper.GetTimeStampInMilliseconds();
        var duration = getRecordTime - blockTime;

        if (duration > _maxDuration)
        {
            _logger.LogWarning(
                "sync data duration too large, {blockHeight}, caHash: {caHash}, changeType: {changeType}, blockTime: {blockTime}, getRecordTime: {getRecordTime}",
                syncRecord.BlockHeight, syncRecord.CaHash, syncRecord.ChangeType, block.Header.Time, getRecordTime);
            return;
        }

        syncRecord.DataSyncMonitor = new DataSyncMonitor()
        {
            CaHash = syncRecord.CaHash,
            ChangeType = syncRecord.ChangeType,
            StartTime = block.Header.Time,
            MonitorNodes = new List<MonitorNode>()
        };

        syncRecord.DataSyncMonitor.MonitorNodes.Add(new MonitorNode()
        {
            Name = DataSyncType.RegisterChainBlock.ToString(),
            StartTime = blockTime
        });

        syncRecord.DataSyncMonitor.MonitorNodes.Add(new MonitorNode()
        {
            Name = DataSyncType.GetRecord.ToString(),
            StartTime = getRecordTime,
            Duration = (int)duration
        });
    }

    public void AddNode(SyncRecord record, DataSyncType syncType)
    {
        if (record.DataSyncMonitor == null || record.DataSyncMonitor.MonitorNodes.IsNullOrEmpty()) return;

        var startTime = TimeHelper.GetTimeStampInMilliseconds();
        var lastNode = record.DataSyncMonitor.MonitorNodes.Last();
        // need to consider retry status.
        record.DataSyncMonitor.MonitorNodes.Add(new MonitorNode()
        {
            Name = syncType.ToString(),
            StartTime = startTime,
            Duration = (int)(startTime - lastNode.StartTime)
        });
    }

    public async Task FinishAsync(SyncRecord record, string endChainId, long endHeight)
    {
        try
        {
            if (!Check(record)) return;

            AddNode(record, DataSyncType.EndSync);
            var endBlock = await _contractProvider.GetBlockByHeightAsync(endChainId, endHeight);
            var blockInterval = endBlock.Header.Time - record.DataSyncMonitor.StartTime;
            record.DataSyncMonitor.TotalTime = (int)blockInterval.TotalMilliseconds;
            AddFinishNode(record, TimeHelper.GetTimeStampFromDateTime(endBlock.Header.Time));

            WriteMonitorLog(record);
            AddSyncLog(record);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "write monitor log fail, caHash: {caHash}, changeType: {changeType}",
                record.CaHash, record.ChangeType);
        }
    }

    private void AddFinishNode(SyncRecord record, long blockTime)
    {
        var lastNode = record.DataSyncMonitor.MonitorNodes.Last();
        record.DataSyncMonitor.MonitorNodes.Add(new MonitorNode()
        {
            Name = DataSyncType.SyncChainBlock.ToString(),
            StartTime = blockTime,
            Duration = (int)Math.Abs(blockTime - lastNode.StartTime)
        });
    }

    private bool Check(SyncRecord record) => _indicatorLogger.IsEnabled() && record.DataSyncMonitor != null;


    private void WriteMonitorLog(SyncRecord record)
    {
        foreach (var node in record.DataSyncMonitor.MonitorNodes)
        {
            if (node.Duration == 0) continue;
            AddMonitorLog(node.Name, node.Duration);
        }
    }

    private void AddMonitorLog(string changeType, int duration)
    {
        _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, changeType, duration);
    }

    private void AddSyncLog(SyncRecord record)
    {
        var logInfo = JsonConvert.SerializeObject(record.DataSyncMonitor);
        _logger.LogInformation("[SyncRecordLog] caHash: {caHash}, changeType: {changeType}, logInfo: {logInfo}",
            record.CaHash, record.ChangeType, logInfo);
    }

    public async Task AddMonitorLogAsync(string startChainId, long startHeight, string endChainId, long endHeight,
        string changeType)
    {
        try
        {
            if (!_indicatorLogger.IsEnabled()) return;

            var startBlock = await _contractProvider.GetBlockByHeightAsync(startChainId, startHeight);
            var endBlock = await _contractProvider.GetBlockByHeightAsync(endChainId, endHeight);
            var blockInterval = endBlock.Header.Time - startBlock.Header.Time;
            var duration = (int)blockInterval.TotalMilliseconds;

            if (duration < 0) return;
            _indicatorLogger.LogInformation(MonitorTag.ChainDataSync, changeType, duration);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add monitor log error.");
        }
    }

    public async Task AddHeightIndexMonitorLogAsync(string chainId, long indexHeight)
    {
        try
        {
            if (!_indicatorLogger.IsEnabled()) return;

            var height = await _contractProvider.GetBlockHeightAsync(chainId);
            var duration = (int)Math.Abs(height - indexHeight);
            _indicatorLogger.LogInformation(MonitorTag.DataSyncHeightIndex, chainId,
                duration);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add height index monitor log error.");
        }
    }
}