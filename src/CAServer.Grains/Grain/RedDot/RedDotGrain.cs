using CAServer.EnumType;
using CAServer.Grains.State.RedDot;
using CAServer.RedDot.Dtos;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.RedDot;

public class RedDotGrain : Grain<RedDotState>, IRedDotGrain
{
    private readonly IObjectMapper _objectMapper;

    public RedDotGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

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

    public Task<GrainResultDto<RedDotInfo>> GetRedDotInfo(RedDotType redDotType)
    {
        var result = new GrainResultDto<RedDotInfo>();
        var redDotInfo = State.RedDotInfos.FirstOrDefault(t => t.RedDotType == redDotType);

        result.Success = redDotInfo == null;
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<RedDotGrainDto>> SetRedDot(RedDotType redDotType)
    {
        SetStateId();
        var result = new GrainResultDto<RedDotGrainDto>();
        var redDotInfo = State.RedDotInfos.FirstOrDefault(t => t.RedDotType == redDotType);
        if (redDotInfo == null)
        {
            State.RedDotInfos.Add(new RedDotInfo
            {
                RedDotType = redDotType,
                Status = RedDotStatus.Read,
                CreateTime = DateTime.UtcNow,
                ReadTime = DateTime.UtcNow
            });
        }
        else
        {
            redDotInfo.Status = RedDotStatus.Read;
        }

        result.Success = true;
        result.Data = _objectMapper.Map<RedDotState, RedDotGrainDto>(State);
        return Task.FromResult(result);
    }

    private void SetStateId()
    {
        if (!State.Id.IsNullOrEmpty()) return;

        State.Id = this.GetPrimaryKeyString();
    }
}