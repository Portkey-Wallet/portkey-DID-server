using CAServer.Grains.State.ApplicationHandler;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface  ISyncRecordGrain : IGrainWithStringKey
{
    Task AddRecordsAsync(List<SyncRecord> records);
    Task<List<SyncRecord>> GetRecordsAsync();

    Task AddFailedRecordsAsync(List<SyncRecord> record);
    Task<List<SyncRecord>> GetFailedRecordsAsync();
    Task ClearRecords();
    Task ClearFailedRecords();
}

public class SyncRecordGrain : Grain<SyncRecordState>, ISyncRecordGrain
{
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        
        State.FailedRecords ??= new List<SyncRecord>();
        State.Records ??= new List<SyncRecord>();

        return base.OnActivateAsync();
    }

    public async Task AddRecordsAsync(List<SyncRecord> records)
    {
        if (!records.IsNullOrEmpty())
        {
            State.Records.AddRange(records);
        }
        
        await WriteStateAsync();
    }

    public Task<List<SyncRecord>> GetRecordsAsync()
    {
        return Task.FromResult(State.Records);
    }

    public async Task AddFailedRecordsAsync(List<SyncRecord> records)
    {
        if (!records.IsNullOrEmpty())
        {
            State.FailedRecords.AddRange(records);
        }
       
        await WriteStateAsync();
    }

    public Task<List<SyncRecord>> GetFailedRecordsAsync()
    {
        return Task.FromResult(State.FailedRecords);
    }

    public async Task ClearRecords()
    {
        State.Records = new List<SyncRecord>();
        await WriteStateAsync();
    }
    
    public async Task ClearFailedRecords()
    {
        State.FailedRecords = new List<SyncRecord>();
        await WriteStateAsync();
    }
}