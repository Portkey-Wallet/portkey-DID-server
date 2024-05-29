using CAServer.Grains.State.Market;
using CAServer.Market;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Market;

public class UserMarketTokenFavoritesGrain : Grain<UserMarketTokenFavoritesState>, IUserMarketTokenFavoritesGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserMarketTokenFavoritesGrain(IObjectMapper objectMapper)
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
    
    public async Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserCollectTokenAsync(
        UserMarketTokenFavoritesDto userMarketTokenFavorites)
    {
        var result = new GrainResultDto<UserMarketTokenFavoritesGrainDto>();
        if (userMarketTokenFavorites.UserId.Equals(State.UserId) && State.Favorites.Any(f => f.CoingeckoId.Equals(userMarketTokenFavorites.CoingeckoId) && f.Collected))
        {
            result.Success = false;
            result.Message = "user has collected this token";
            return result;
        }
        State.Id = this.GetPrimaryKey();
        State.UserId = userMarketTokenFavorites.UserId;
        var items = new MarketToken()
        {
            CoingeckoId = userMarketTokenFavorites.CoingeckoId,
            Symbol = userMarketTokenFavorites.Symbol,
            Collected = userMarketTokenFavorites.Collected,
            CollectTimestamp = userMarketTokenFavorites.CollectTimestamp
        };
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserCancelFavoriteTokenAsync(Guid userId, string coingeckoId)
    {
        var result = new GrainResultDto<UserMarketTokenFavoritesGrainDto>();
        if (!userId.Equals(State.UserId) || !State.Favorites.Any(f => f.CoingeckoId.Equals(coingeckoId) && f.Collected))
        {
            result.Success = false;
            result.Message = "there is no record";
            return result;
        }

        var item = State.Favorites.Find(f => f.CoingeckoId.Equals(coingeckoId) && f.Collected);
        item.Collected = false;
        await WriteStateAsync();
        
        result.Success = true;
        result.Data = _objectMapper.Map<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>(State);
        return result;
    }
    
    public async Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> UserReCollectFavoriteTokenAsync(Guid userId, string coingeckoId)
    {
        var result = new GrainResultDto<UserMarketTokenFavoritesGrainDto>();
        if (!userId.Equals(State.UserId) || !State.Favorites.Any(f => f.CoingeckoId.Equals(coingeckoId) && !f.Collected))
        {
            result.Success = false;
            result.Message = "there is no record";
            return result;
        }
        var item = State.Favorites.Find(f => f.CoingeckoId.Equals(coingeckoId) && f.Collected);
        item.Collected = true;
        await WriteStateAsync();
        
        result.Success = true;
        result.Data = _objectMapper.Map<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>(State);
        return result;
        
    }
    
    public async Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> GetUserTokenFavorites(Guid userId, string coingeckoId, bool collected)
    {
        var result = new GrainResultDto<UserMarketTokenFavoritesGrainDto>();
        if (userId.Equals(State.UserId) && State.Favorites.Any(f => f.CoingeckoId.Equals(coingeckoId) && f.Collected.Equals(collected)))
        {
            result.Success = true;
            result.Data = _objectMapper.Map<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>(State);
            return result;
        }

        result.Success = false;
        result.Message = " there is no record";
        return result;
    }

    public async Task<GrainResultDto<UserMarketTokenFavoritesGrainDto>> ListUserFavoritesToken(Guid userId)
    {
        var result = new GrainResultDto<UserMarketTokenFavoritesGrainDto>();
        result.Success = true;
        result.Data = _objectMapper.Map<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>(State);
        return result;
    }
}