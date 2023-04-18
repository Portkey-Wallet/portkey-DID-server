using CAServer.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public interface IUserTokenGrain : IGrainWithGuidKey
{
    Task<UserToken> AddUserTokenAsync(Guid userId,Token tokenItem);
    Task ChangeTokenDisplayAsync(bool isDisplay);
}