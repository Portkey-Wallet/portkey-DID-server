using System;
using System.Collections.Generic;
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
    Task SetValidatedRecordsAsyncEmpty(string chainId);
    Task SetToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records);
    Task SetToBeValidatedRecordsAsyncEmpty(string chainId);
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
        _logger.LogInformation($"IRecordsBucketContainer init MaxBucket {_indexOptions.MaxBucket}");
    }

    public async Task AddValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        if (records.IsNullOrEmpty())
        {
            return;
        }

        foreach (var syncRecord in records)
        {
            _logger.LogInformation($"IRecordsBucketContainer AddValidatedRecordsAsync to Chain: {chainId} syncRecord = {syncRecord.CaHash} {syncRecord
                .BlockHeight}");
        }
        try
        {
            var dict = GetSyncRecordBucketDictionary(records,_indexOptions.MaxBucket);

            foreach (var bucket in dict)
            {
                var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                    GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
                await grain.AddValidatedRecordsAsync(bucket.Value);
            }

            _logger.LogInformation("IRecordsBucketContainer AddValidatedRecordsAsync to Chain: {id} Success", chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IRecordsBucketContainer AddValidatedRecordsAsync to Chain: {id} error, {records}", chainId,records.Count.ToString());
        }
    }

    public async Task AddToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        if (records.IsNullOrEmpty())
        {
            return;
        }
        foreach (var syncRecord in records)
        {
            _logger.LogInformation($"IRecordsBucketContainer AddToBeValidatedRecordsAsync to Chain: {chainId} syncRecord = {syncRecord.CaHash} {syncRecord
                .BlockHeight}");
        }
        try
        {
            var dict = GetSyncRecordBucketDictionary(records,_indexOptions.MaxBucket);

            foreach (var bucket in dict)
            {
                var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                    GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
                await grain.AddToBeValidatedRecordsAsync(bucket.Value);
            }

            _logger.LogInformation("IRecordsBucketContainer AddToBeValidatedRecordsAsync to Chain: {id} Success", chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IRecordsBucketContainer AddToBeValidatedRecordsAsync to Chain: {id} error, {records}", chainId, records.Count.ToString());
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
        if (records.IsNullOrEmpty())
        {
            return;
        }
        foreach (var syncRecord in records)
        {
            _logger.LogInformation($"IRecordsBucketContainer SetValidatedRecordsAsync to Chain: {chainId} syncRecord = {syncRecord.CaHash} {syncRecord.BlockHeight}");
        }
        
        var dict = GetSyncRecordBucketDictionary(records,_indexOptions.MaxBucket);
        foreach (var bucket in dict)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
            try
            {
                await grain.SetValidatedRecords(bucket.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "IRecordsBucketContainer SetValidatedRecordsAsync to Chain: {id} error, {records}", chainId, bucket.Value.Count.ToString());
                throw;
            }
        }
    }
    public async Task SetValidatedRecordsAsyncEmpty(string chainId)
    {
        for (int i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            await grain.SetValidatedRecords(new List<SyncRecord>());
        }
    }

    public async Task SetToBeValidatedRecordsAsync(string chainId, List<SyncRecord> records)
    {
        if (records.IsNullOrEmpty())
        {
            return;
        }
        foreach (var syncRecord in records)
        {
            _logger.LogInformation($"IRecordsBucketContainer SetToBeValidatedRecordsAsync to Chain: {chainId} syncRecord = {syncRecord.CaHash} {syncRecord.BlockHeight}");
        }
        var dict = GetSyncRecordBucketDictionary(records,_indexOptions.MaxBucket);
        foreach (var bucket in dict)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, bucket.Key));
            try
            {
                await grain.SetToBeValidatedRecords(bucket.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "IRecordsBucketContainer SetToBeValidatedRecordsAsync to Chain: {id} error, {records}", chainId, bucket.Value.Count.ToString());
                throw;
            }
        }
    }
    public async Task SetToBeValidatedRecordsAsyncEmpty(string chainId)
    {
        for (int i = 0; i < _indexOptions.MaxBucket; i++)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(
                GrainIdHelper.GenerateGrainId(GrainId.SyncRecord, chainId, i.ToString()));
            await grain.SetToBeValidatedRecords(new List<SyncRecord>());
        }
    }

    private string GetSyncRecordBucket(SyncRecord record, int maxBucket)
    {
        return (record.CaHash.GetHashCode() % maxBucket).ToString();
    }

    private Dictionary<string, List<SyncRecord>> GetSyncRecordBucketDictionary(List<SyncRecord> records, int maxBucket)
    {
        var dict = new Dictionary<string, List<SyncRecord>>();
        foreach (var record in records)
        {
            var bucket = GetSyncRecordBucket(record, maxBucket);
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