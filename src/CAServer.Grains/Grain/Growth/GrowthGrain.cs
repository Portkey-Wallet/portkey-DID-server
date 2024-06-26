using CAServer.Commons;
using CAServer.Grains.State.Growth;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Growth;

public class GrowthGrain : Grain<GrowthState>, IGrowthGrain
{
    private readonly IObjectMapper _objectMapper;

    public GrowthGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

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

    public async Task<GrainResultDto<GrowthGrainDto>> CreateGrowthInfo(GrowthGrainDto growthGrainDto)
    {
        var result = new GrainResultDto<GrowthGrainDto>();
        if (!State.Id.IsNullOrEmpty())
        {
            result.Success = true;
            result.Data = _objectMapper.Map<GrowthState, GrowthGrainDto>(State);
            return result;
        }

        State = _objectMapper.Map<GrowthGrainDto, GrowthState>(growthGrainDto);
        State.Id = this.GetPrimaryKeyString();

        var inviteCodeGrain = GetInviteCodeGrain();
        State.InviteCode = await inviteCodeGrain.GenerateInviteCode();
        State.CreateTime = DateTime.UtcNow;

        result.Success = true;
        result.Data = _objectMapper.Map<GrowthState, GrowthGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<GrowthGrainDto>> GetGrowthInfo()
    {
        var result = new GrainResultDto<GrowthGrainDto>();
        if (string.IsNullOrEmpty(State.Id) || State.IsDeleted)
        {
            result.Message = "Growth info not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<GrowthState, GrowthGrainDto>(State);
        return Task.FromResult(result);
    }

    public Task<bool> Exist()
    {
        return Task.FromResult(!State.Id.IsNullOrEmpty() && !State.IsDeleted);
    }

    private IInviteCodeGrain GetInviteCodeGrain()
    {
        return GrainFactory.GetGrain<IInviteCodeGrain>(CommonConstant.InviteCodeGrainId);
    }
}