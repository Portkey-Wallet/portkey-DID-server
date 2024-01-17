using CAServer.Commons;
using CAServer.Grains.State.Growth;
using Orleans;
using Volo.Abp;

namespace CAServer.Grains.Grain.Growth;

public interface IInviteCodeGrain : IGrainWithStringKey
{
    Task<string> GenerateInviteCode();
}

public class InviteCodeGrain : Grain<InviteCodeState>, IInviteCodeGrain
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
    
    public Task<string> GenerateInviteCode()
    {
        if (this.GetPrimaryKeyString() != CommonConstant.InviteCodeGrainId)
        {
            throw new UserFriendlyException("invalid grain id");
        }
        return Task.FromResult((++State.CurrentInviteCode).ToString());
    }
}