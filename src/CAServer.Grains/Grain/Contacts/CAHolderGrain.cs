using CAServer.CAAccount.Dtos;
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
        State.CaAddress = caHolderDto.CaAddress;
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

    public async Task<GrainResultDto<CAHolderGrainDto>> DeleteAsync()
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return result;
        }

        State.IsDeleted = true;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }

    public Task<string> GetCAHashAsync()
    {
        return Task.FromResult(State.CaHash);
    }

    public Task<GrainResultDto<CAHolderGrainDto>> GetCaHolder()
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<CAHolderGrainDto>> UpdateHolderInfo(HolderInfoDto holderInfo)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return result;
        }

        if (!holderInfo.NickName.IsNullOrWhiteSpace())
        {
            State.Nickname = holderInfo.NickName;
        }

        if (!holderInfo.Avatar.IsNullOrWhiteSpace())
        {
            State.Avatar = holderInfo.Avatar;
        }
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }
}