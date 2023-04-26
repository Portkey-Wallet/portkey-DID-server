using CAServer.Grains.State.ApplicationHandler;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface  ISyncRecordGrain : IGrainWithStringKey
{
    Task AddValidatedRecordsAsync(List<SyncRecord> records);
    Task<List<SyncRecord>> GetValidatedRecordsAsync();

    Task AddToBeValidatedRecordsAsync(List<SyncRecord> record);
    Task<List<SyncRecord>> GetToBeValidatedRecordsAsync();
    Task ClearValidatedRecords();
    Task ClearToBeValidatedRecords();
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

    public async Task ClearValidatedRecords()
    {
        State.ValidatedRecords = new List<SyncRecord>();
        await WriteStateAsync();
    }
    
    public async Task ClearToBeValidatedRecords()
    {
        State.ToBeValidatedRecords = new List<SyncRecord>();
        await WriteStateAsync();
    }
}