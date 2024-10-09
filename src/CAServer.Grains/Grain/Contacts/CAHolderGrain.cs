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
        State.CaHash = caHolderDto.CaHash;
        State.Nickname = caHolderDto.Nickname;
        State.ModifiedNickname = caHolderDto.ModifiedNickname;
        State.PopedUp = caHolderDto.PopedUp;
        State.IdentifierHash = caHolderDto.IdentifierHash;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<CAHolderGrainDto>> AddHolderWithAvatarAsync(CAHolderGrainDto caHolderDto)
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
        State.ModifiedNickname = caHolderDto.ModifiedNickname;
        State.PopedUp = caHolderDto.PopedUp;
        State.IdentifierHash = caHolderDto.IdentifierHash;
        State.Avatar = caHolderDto.Avatar;

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
        State.ModifiedNickname = true;
        State.IdentifierHash = string.Empty;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAndMarkBitAsync(string nickname, bool modifiedNickname, string identifierHash)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return result;
        }

        State.Nickname = nickname;
        State.ModifiedNickname = modifiedNickname;
        State.IdentifierHash = identifierHash;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }
    
    public async Task UpdatePopUpAsync(bool poppedUp)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return ;
        }

        State.PopedUp = poppedUp;
        await WriteStateAsync();
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

        State.ModifiedNickname = true;
        State.IdentifierHash = string.Empty;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }
    
    public async Task<GrainResultDto<CAHolderGrainDto>> AppendOrUpdateSecondaryEmailAsync(string secondaryEmail)
    {
        var result = new GrainResultDto<CAHolderGrainDto>();
        if (string.IsNullOrWhiteSpace(State.CaHash))
        {
            result.Message = CAHolderMessage.NotExistMessage;
            return result;
        }

        State.SecondaryEmail = secondaryEmail;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CAHolderState, CAHolderGrainDto>(State);
        return result;
    }
}