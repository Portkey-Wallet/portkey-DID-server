using CAServer.Commons;
using CAServer.Grains.State.Growth;
using Volo.Abp;

namespace CAServer.Grains.Grain.Growth;

public interface IInviteCodeGrain : IGrainWithStringKey
{
    Task<string> GenerateInviteCode();
}

public class InviteCodeGrain : Grain<InviteCodeState>, IInviteCodeGrain
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

    public async Task<string> GenerateInviteCode()
    {
        if (this.GetPrimaryKeyString() != CommonConstant.InviteCodeGrainId)
        {
            throw new UserFriendlyException("invalid grain id");
        }

        State.CurrentInviteCode += 1;
        await WriteStateAsync();
        return State.CurrentInviteCode.ToString();
    }
}