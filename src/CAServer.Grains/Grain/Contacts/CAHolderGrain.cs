using CAServer.Grains.State;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Contacts;

public class CAHolderGrain : Grain<CAHolderState>, ICAHolderGrain
{
    private readonly IObjectMapper _objectMapper;

    public CAHolderGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<CAHolderGrainDto>> AddHolderAsync(CAHolderGrainDto caHolderDto)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (!string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.ExistedMessage;
            return result;
        }

        State.Id = this.GetPrimaryKey();
        State.UserId = caHolderDto.UserId;
        State.CreateTime = caHolderDto.CreateTime;
        State.CaHash = caHolderDto.CaHash;
        State.Nickname = caHolderDto.Nickname;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAsync(string nickname)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return result;
        }

        State.Nickname = nickname;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }

    public Task<string> GetCAHashAsync()
    {
        return Task.FromResult(State.CaHash);
    }
}