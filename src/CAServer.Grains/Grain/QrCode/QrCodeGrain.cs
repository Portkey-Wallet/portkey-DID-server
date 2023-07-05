using CAServer.Grains.State.QrCode;
using Orleans;

namespace CAServer.Grains.Grain.QrCode;

public class QrCodeGrain : Grain<QrCodeState>, IQrCodeGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public Task<bool> Exist()
    {
        if (string.IsNullOrEmpty(State.Id))
        {
            AddQrCode();
            return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }

    private Task AddQrCode()
    {
        if (string.IsNullOrEmpty(State.Id))
        {
            State.Id = this.GetPrimaryKeyString();
            State.ScanTime = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }
}