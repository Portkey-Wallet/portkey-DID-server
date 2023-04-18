using Orleans;

namespace CAServer.Grains.Grain.Tokens.UserTokens;

public interface IUserTokenSymbolGrain : IGrainWithStringKey
{
    Task<bool> AddUserTokenSymbolAsync(Guid userId, string chainId,string symbol);
    Task<bool> IsUserTokenSymbolExistAsync(string chainId,string symbol);
}