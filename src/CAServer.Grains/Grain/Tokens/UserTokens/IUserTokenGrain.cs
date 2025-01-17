namespace CAServer.Grains.Grain.Tokens.UserTokens;

public interface IUserTokenGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<UserTokenGrainDto>> GetUserToken();
    Task<GrainResultDto<UserTokenGrainDto>> AddUserTokenAsync(Guid userId, UserTokenGrainDto tokenItem);
    Task<GrainResultDto<UserTokenGrainDto>> ChangeTokenDisplayAsync(Guid userId, bool isDisplay, bool isDelete = false);
}