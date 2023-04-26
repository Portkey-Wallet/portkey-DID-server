using CAServer.Grains.State.ApplicationHandler;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface  ISyncRecordGrain : IGrainWithStringKey
{
    Task AddValidatedRecordsAsync(List<SyncRecord> records);
    Task<List<SyncRecord>> GetValidatedRecordsAsync();

    Task AddToBeValidatedRecordsAsync(List<SyncRecord> records);
    Task<List<SyncRecord>> GetToBeValidatedRecordsAsync();
    Task SetValidatedRecords(List<SyncRecord> records);
    Task SetToBeValidatedRecords(List<SyncRecord> records);
}

public class SyncRecordGrain : Grain<SyncRecordState>, ISyncRecordGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        
        State.ToBeValidatedRecords ??= new List<SyncRecord>();
        State.ValidatedRecords ??= new List<SyncRecord>();

        return base.OnActivateAsync();
    }

    public async Task AddValidatedRecordsAsync(List<SyncRecord> records)
    {
        if (!records.IsNullOrEmpty())
        {
            State.ValidatedRecords.AddRange(records);
        }
        
        await WriteStateAsync();
    }

    public Task<List<SyncRecord>> GetValidatedRecordsAsync()
    {
        return Task.FromResult(State.ValidatedRecords);
    }

    public async Task AddToBeValidatedRecordsAsync(List<SyncRecord> records)
    {
        if (!records.IsNullOrEmpty())
        {
            State.ToBeValidatedRecords.AddRange(records);
        }
       
        await WriteStateAsync();
    }

    public Task<List<SyncRecord>> GetToBeValidatedRecordsAsync()
    {
        return Task.FromResult(State.ToBeValidatedRecords);
    }

    public async Task SetValidatedRecords(List<SyncRecord> records)
    {
        State.ValidatedRecords = records;
        await WriteStateAsync();
    }
    
    public async Task SetToBeValidatedRecords(List<SyncRecord> records)
    {
        State.ToBeValidatedRecords = records;
        await WriteStateAsync();
    }
}