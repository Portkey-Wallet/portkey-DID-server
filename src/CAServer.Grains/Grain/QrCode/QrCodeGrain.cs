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

    public Task<GrainResultDto> AddQrCode()
    {
        var result = new GrainResultDto();
        if (string.IsNullOrEmpty(State.Id))
        {
            State.Id = this.GetPrimaryKeyString();
            State.ScanTime = DateTime.UtcNow;
            result.Success = true;
        }

        return Task.FromResult(result);
    }
}