using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IRecordsBucketContainer : ISingletonDependency
{
    Task AddValidatedRecordsAsync(string chainId, List<SyncRecord> records);
    Task AddToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records);
    Task<List<SyncRecord>> GetValidatedRecordsAsync(string chainId);
    Task<List<SyncRecord>> GetToBeValidatedRecordsAsync(string chainId);
    Task SetValidatedRecordsAsync(string chainId, List<SyncRecord> records);
    Task SetToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records);
}

public class RecordsBucketContainer : IRecordsBucketContainer
{
    private readonly ILogger<RecordsBucketContainer> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IndexOptions _indexOptions;

    public RecordsBucketContainer(ILogger<RecordsBucketContainer> logger, IOptionsSnapshot<IndexOptions> indexOptions,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _indexOptions = indexOptions.Value;
        _clusterClient = clusterClient;
    }

    public async Task AddValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        if (records.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            var dict = GetSyncRecordBucketDictionary(records);

            foreach (var bucket in dict)
            {
                var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                    GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
                await grain.AddValidatedRecordsAsync(bucket.Value);
            }

            _logger.LogInformation("Set ValidatedRecords to Chain: {id} Success", chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Set ValidatedRecords to Chain: {id} Failed, {records}", chainId,
                JsonConvert.SerializeObject(records));
        }
    }

    public async Task AddToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        if (records.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            var dict = GetSyncRecordBucketDictionary(records);

            foreach (var bucket in dict)
            {
                var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                    GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
                await grain.AddToBeValidatedRecordsAsync(bucket.Value);
            }

            _logger.LogInformation("Set ToBeValidatedRecords to Chain: {id} Success", chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Set ToBeValidatedRecords to Chain: {id} Failed, {records}", chainId,
                JsonConvert.SerializeObject(records));
        }
    }

    public async Task<List<SyncRecord>> GetValidatedRecordsAsync(string chainId)
    {
        var list = new List<SyncRecord>();

        for (var i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            var records = await grain.GetValidatedRecordsAsync();

            if (!records.IsNullOrEmpty())
            {
                list.AddRange(records);
            }
        }

        return list;
    }

    public async Task<List<SyncRecord>> GetToBeValidatedRecordsAsync(string chainId)
    {
        var list = new List<SyncRecord>();

        for (var i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            var records = await grain.GetToBeValidatedRecordsAsync();

            if (!records.IsNullOrEmpty())
            {
                list.AddRange(records);
            }
        }

        return list;
    }

    public async Task SetValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        for (var i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            await grain.SetValidatedRecords(records);
        }
    }

    public async Task SetToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        var lists = records
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / _indexOptions.BatchNumber)
            .Select(group => group.Select(x => x.item).ToList())
            .ToList();
        for (var i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            foreach (var list in lists)
            {
                await grain.SetToBeValidatedRecords(list);
            }
        }
    }
    
    private string GetSyncRecordBucket(SyncRecord record)
    {
        return "0";
    }

    private Dictionary<string, List<SyncRecord>> GetSyncRecordBucketDictionary(List<SyncRecord> records)
    {
        var dict = new Dictionary<string, List<SyncRecord>>();
        foreach (var record in records)
        {
            var bucket = GetSyncRecordBucket(record);
            if (dict.TryGetValue(bucket, out var value))
            {
                value.Add(record);
            }
            else
            {
                dict.Add(bucket, new List<SyncRecord> { record });
            }
        }

        return dict;
    }
}