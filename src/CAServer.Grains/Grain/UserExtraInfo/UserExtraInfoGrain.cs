using CAServer.CAAccount.Dtos;
using CAServer.Grains.State.UserExtraInfo;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.UserExtraInfo;

public class UserExtraInfoGrain : Grain<UserExtraInfoState>, IUserExtraInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserExtraInfoGrain(IObjectMapper objectMapper)
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

    public async Task<UserExtraInfoGrainDto> AddOrUpdateAsync(UserExtraInfoGrainDto userExtraInfoGrainDto)
    {
        if (string.IsNullOrWhiteSpace(State.Id))
        {
            State = _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoState>(userExtraInfoGrainDto);
            State.Id = this.GetPrimaryKeyString();

            await WriteStateAsync();
            return _objectMapper.Map<UserExtraInfoState, UserExtraInfoGrainDto>(State);
        }

        if (State.GuardianType == GuardianIdentifierType.Google.ToString()
            || State.GuardianType == GuardianIdentifierType.Telegram.ToString()
            || State.GuardianType == GuardianIdentifierType.Facebook.ToString()
            )
        {
            State.FullName = userExtraInfoGrainDto.FullName;
            State.FirstName = userExtraInfoGrainDto.FirstName;
            State.LastName = userExtraInfoGrainDto.LastName;
            State.Picture = userExtraInfoGrainDto.Picture;
        }

        State.Email = userExtraInfoGrainDto.Email;
        State.VerifiedEmail = userExtraInfoGrainDto.VerifiedEmail;
        State.IsPrivateEmail = userExtraInfoGrainDto.IsPrivateEmail;

        await WriteStateAsync();
        return _objectMapper.Map<UserExtraInfoState, UserExtraInfoGrainDto>(State);
    }

    public async Task<UserExtraInfoGrainDto> AddOrUpdateAppleUserAsync(UserExtraInfoGrainDto userExtraInfoGrainDto)
    {
        State = _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoState>(userExtraInfoGrainDto);
        State.Id = this.GetPrimaryKeyString();

        await WriteStateAsync();
        return _objectMapper.Map<UserExtraInfoState, UserExtraInfoGrainDto>(State);
    }

    public Task<GrainResultDto<UserExtraInfoGrainDto>> GetAsync()
    {
        var result = new GrainResultDto<UserExtraInfoGrainDto>();

        if (string.IsNullOrWhiteSpace(State.Id))
        {
            result.Message = "User not exist.";
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<UserExtraInfoState, UserExtraInfoGrainDto>(State);

        return Task.FromResult(result);
    }
}