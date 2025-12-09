using CAServer.Market;

namespace CAServer.Grains.Grain.Market;

public interface IUserMarketTokenFavoritesGrain : IGrainWithGuidKey
{
    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserCollectTokenAsync(UserMarketTokenFavoritesDto userMarketTokenFavorites);

    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserCollectDefaultTokenAsync(
        UserDefaultFavoritesDto userDefaultFavorites);
    
    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserCancelFavoriteTokenAsync(Guid userId, string coingeckoId);

    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserReCollectFavoriteTokenAsync(Guid userId, string coingeckoId);

    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> GetUserTokenFavorites(Guid userId, string coingeckoId, bool collected);

    public Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> ListUserFavoritesToken(Guid userId);
}