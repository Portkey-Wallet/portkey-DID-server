using CAServer.Commons;
using CAServer.Grains.State.Growth;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Growth;

public class GrowthGrain : Grain<GrowthState>, IGrowthGrain
{
    private readonly IObjectMapper _objectMapper;

    public GrowthGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<GrowthGrainDto>> CreateGrowthInfo(GrowthGrainDto growthGrainDto)
    {
        var result = new GrainResultDto<GrowthGrainDto>();
        if (Exist(growthGrainDto.ProjectCode).Result)
        {
            result.Message = "Growth info already exists.";
            return result;
        }

        var inviteCodeGrain = GetInviteCodeGrain();
        var inviteCode = await inviteCodeGrain.GenerateInviteCode();
        if (!State.Id.IsNullOrEmpty())
        {
            // add in InviteInfos
            var grainDto = await AddInviteInfo(growthGrainDto, inviteCode);
            return new GrainResultDto<GrowthGrainDto>(grainDto);
        }

        State = _objectMapper.Map<GrowthGrainDto, GrowthState>(growthGrainDto);
        State.Id = this.GetPrimaryKeyString();

        State.InviteCode = inviteCode;
        State.CreateTime = DateTime.UtcNow;
        await WriteStateAsync();
        
        result.Success = true;
        result.Data = _objectMapper.Map<GrowthState, GrowthGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<GrowthGrainDto>> GetGrowthInfo(string projectCode)
    {
        var result = new GrainResultDto<GrowthGrainDto>();
        if (!Exist(projectCode).Result)
        {
            result.Message = "Growth info not exists.";
            return Task.FromResult(result);
        }

        if (State.ProjectCode == projectCode)
        {
            result.Data = _objectMapper.Map<GrowthState, GrowthGrainDto>(State);
        }
        else
        {
            var inviteInfo = State.InviteInfos.First(t => t.ProjectCode == projectCode);
            var grainDto = _objectMapper.Map<InviteInfo, GrowthGrainDto>(inviteInfo);
            grainDto.UserId = State.UserId;
            grainDto.CaHash = State.CaHash;
            result.Data = grainDto;
        }

        result.Success = true;
        return Task.FromResult(result);
    }

    public Task<bool> Exist(string projectCode)
    {
        if (!State.ProjectCode.IsNullOrEmpty() && State.ProjectCode == projectCode)
        {
            return Task.FromResult(true);
        }

        var inviteInfo = State.InviteInfos?.FirstOrDefault(t => t.ProjectCode == projectCode);
        return Task.FromResult(inviteInfo != null);
    }

    private IInviteCodeGrain GetInviteCodeGrain()
    {
        return GrainFactory.GetGrain<IInviteCodeGrain>(CommonConstant.InviteCodeGrainId);
    }

    private async Task<GrowthGrainDto> AddInviteInfo(GrowthGrainDto growthGrainDto, string inviteCode)
    {
        State.InviteInfos ??= new List<InviteInfo>();
        var inviteInfo = _objectMapper.Map<GrowthGrainDto, InviteInfo>(growthGrainDto);
        inviteInfo.Id = $"{State.Id}-{growthGrainDto.ProjectCode}";
        inviteInfo.InviteCode = inviteCode;
        inviteInfo.CreateTime = DateTime.UtcNow;
        State.InviteInfos.Add(inviteInfo);
        await WriteStateAsync();
        
        var grainDto = _objectMapper.Map<InviteInfo, GrowthGrainDto>(inviteInfo);
        grainDto.UserId = State.UserId;
        grainDto.CaHash = State.CaHash;
        return grainDto;
    }
}