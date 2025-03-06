namespace CAServer.Grains.Grain.UserExtraInfo;

public interface IUserExtraInfoGrain : IGrainWithStringKey
{
    Task<UserExtraInfoGrainDto> AddOrUpdateAsync(UserExtraInfoGrainDto userExtraInfoGrainDto);
    Task<UserExtraInfoGrainDto> AddOrUpdateAppleUserAsync(UserExtraInfoGrainDto userExtraInfoGrainDto);
    Task<GrainResultDto<UserExtraInfoGrainDto>> GetAsync();
}