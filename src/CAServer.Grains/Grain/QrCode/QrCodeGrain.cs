using CAServer.Grains.State.QrCode;
using Orleans;

namespace CAServer.Grains.Grain.QrCode;

public class QrCodeGrain : Grain<QrCodeState>, IQrCodeGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public Task<bool> AddIfAbsent()
    {
        if (string.IsNullOrEmpty(State.Id))
        {
            State.Id = this.GetPrimaryKeyString();
            State.ScanTime = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
}