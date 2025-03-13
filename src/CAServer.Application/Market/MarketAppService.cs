using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Grains.Grain.Market;
using CAServer.Market.enums;
using CAServer.Tokens.TokenPrice;
using CAServer.Transfer;
using CAServer.Transfer.Dtos;
using CoinGecko.Entities.Response.Coins;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.Market;

[RemoteService(false)]
[DisableAuditing]
public class MarketAppService : CAServerAppService, IMarketAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IEnumerable<ITokenPriceProvider> _marketDataProviders;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly ILogger<MarketAppService> _logger;
    private readonly ITransferAppService _transferAppService;
    private readonly IMarketCacheProvider _marketCacheProvider;

    public MarketAppService(IObjectMapper objectMapper,
        IClusterClient clusterClient, IEnumerable<ITokenPriceProvider> marketDataProviders,
        IDistributedCache<string> distributedCache, ILogger<MarketAppService> logger,
        ITransferAppService transferAppService,
        IMarketCacheProvider marketCacheProvider)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _marketDataProviders = marketDataProviders;
        _distributedCache = distributedCache;
        _logger = logger;
        _transferAppService = transferAppService;
        _marketCacheProvider = marketCacheProvider;
    }

    public async Task<List<MarketCryptocurrencyDto>> GetMarketCryptocurrencyDataByType(string type, string sort, string sortDir)
    {
        List<MarketCryptocurrencyDto> result = new List<MarketCryptocurrencyDto>();
        if (MarketChosenType.Hot.ToString().Equals(type))
        {
            result = await GetHotListings();
        }
        else if (MarketChosenType.Trending.ToString().Equals(type))
        {
            result = await GetTrendingList();
        }
        else if (MarketChosenType.Favorites.ToString().Equals(type) && CurrentUser.Id.HasValue)
        {
            result = await GetFavoritesList(CurrentUser.GetId());
        }
        else
        {
            return result;
        }

        //deal with the collected logic
        await CollectedStatusHandler(result);

        //deal with the sort strategy 
        if (!sort.IsNullOrEmpty())
        {
            result = CryptocurrencyDataSortHandler(result, sort, sortDir);
        }

        //invoke etransfer for the SupportEtransfer field
        await DealWithEtransferMarkBit(result);
        return result;
    }

    private string GetCachePrefix(MarketChosenType chosenType)
    {
        return "DiscoverMarket:" + chosenType + ":";
    }

    private async Task DealWithEtransferMarkBit(List<MarketCryptocurrencyDto> result)
    {
        try
        {
            var responseFromEtransfer = await _transferAppService.GetTokenOptionListAsync(new GetTokenOptionListRequestDto()
            {
                Type = "Deposit"
            });
            if (responseFromEtransfer != null && responseFromEtransfer.Data != null)
            {
                GetTokenOptionListDto getTokenOptionListDto = responseFromEtransfer.Data;
                var symbolToToken = getTokenOptionListDto.TokenList.ToDictionary(t => t.Symbol, t => t);
                foreach (var marketCryptocurrencyDto in result)
                {
                    if (symbolToToken.ContainsKey(marketCryptocurrencyDto.Symbol.ToUpper()))
                    {
                        marketCryptocurrencyDto.SupportEtransfer = true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "invoke etransfer error");
        }
    }

    private async Task CollectedStatusHandler(List<MarketCryptocurrencyDto> result)
    {
        if (result.IsNullOrEmpty() || !CurrentUser.Id.HasValue)
        {
            return;
        }
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<IUserMarketTokenFavoritesGrain>(userId);
        var grainResultDto = await grain.ListUserFavoritesToken(userId);
        if (!grainResultDto.Success || grainResultDto.Data == null || grainResultDto.Data.Favorites.IsNullOrEmpty())
        {
            return ;
        }

        var coinIdToCollected = grainResultDto.Data.Favorites.ToDictionary(f => f.CoingeckoId, f => f.Collected);
        foreach (var marketCryptocurrencyDto in result)
        {
            if (coinIdToCollected.TryGetValue(marketCryptocurrencyDto.Id, out var collected))
            {
                marketCryptocurrencyDto.Collected = collected;
            }
        }
    }

    private async Task<List<MarketCryptocurrencyDto>> GetHotListings()
    {
        var resultFromCache = await _distributedCache.GetAsync(GetCachePrefix(MarketChosenType.Hot));
        if (!resultFromCache.IsNullOrEmpty())
        {
            return JsonConvert.DeserializeObject<List<MarketCryptocurrencyDto>>(resultFromCache);
        }
        List<MarketCryptocurrencyDto> result = new List<MarketCryptocurrencyDto>();
        foreach (var marketDataProvider in _marketDataProviders)
        {
            try
            {
                List<CoinMarkets> markets = await marketDataProvider.GetHotListingsAsync();
                if (!markets.IsNullOrEmpty())
                {
                    result = _objectMapper.Map<List<CoinMarkets>, List<MarketCryptocurrencyDto>>(markets);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "invoke GetHotListingsAsync error");
            }
        }
        if (!result.IsNullOrEmpty())
        {
            await _distributedCache.SetAsync(GetCachePrefix(MarketChosenType.Hot), JsonConvert.SerializeObject(result), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_marketCacheProvider.GetExpirationMinutes())
            });
        }
        return result;
    }

    private async Task<List<MarketCryptocurrencyDto>> GetTrendingList()
    {
        var resultFromCache = await _distributedCache.GetAsync(GetCachePrefix(MarketChosenType.Trending));
        if (!resultFromCache.IsNullOrEmpty())
        {
            return JsonConvert.DeserializeObject<List<MarketCryptocurrencyDto>>(resultFromCache);
        }
        
        string[] ids = null;
        foreach (var marketDataProvider in _marketDataProviders)
        {
            try
            {
                var trendingList = await marketDataProvider.GetTrendingListingsAsync();
                ids = trendingList.TrendingItems
                    .Select(t => t.TrendingItem)
                    .Select(t => t.Id)
                    .ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "invoke GetTrendingListingsAsync error");
            }
        }

        if (ids == null || ids.Length == 0)
        {
            return new List<MarketCryptocurrencyDto>();
        }

        List<CoinMarkets> coinMarkets = null;
        foreach (var marketDataProvider in _marketDataProviders)
        {
            try
            {
                coinMarkets = await marketDataProvider.GetCoinMarketsByCoinIdsAsync(ids, 15);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "invoke GetHotListingsAsync after trending error");
            }
        }
        List<MarketCryptocurrencyDto> result = _objectMapper.Map<List<CoinMarkets>, List<MarketCryptocurrencyDto>>(coinMarkets);
        if (!result.IsNullOrEmpty())
        {
            await _distributedCache.SetAsync(GetCachePrefix(MarketChosenType.Trending), JsonConvert.SerializeObject(result), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_marketCacheProvider.GetExpirationMinutes())
            });
        }
        return result;
    }

    private async Task<List<MarketCryptocurrencyDto>> GetFavoritesList(Guid userId)
    {
        var resultFromCache = await _distributedCache.GetAsync(GetCachePrefix(MarketChosenType.Favorites) + userId);
        if (!resultFromCache.IsNullOrEmpty())
        {
            return JsonConvert.DeserializeObject<List<MarketCryptocurrencyDto>>(resultFromCache);
        }
        
        var grain = _clusterClient.GetGrain<IUserMarketTokenFavoritesGrain>(userId);
        var result = new List<MarketCryptocurrencyDto>();
        //get user favorites tokens from mongo
        var grainResultDto = await grain.ListUserFavoritesToken(userId);
        if (!grainResultDto.Success || grainResultDto.Data == null)
        {
            return result;
        }

        var favoritesGrainDto = grainResultDto.Data;
        //use the elf and sgr as the default token
        if (grainResultDto.Data.Favorites.IsNullOrEmpty() ||
            !grainResultDto.Data.Favorites.Exists(f => f.CoingeckoId.Equals("aelf"))
            && !grainResultDto.Data.Favorites.Exists(f => f.CoingeckoId.Equals("schrodinger-2")))
        {
            UserDefaultFavoritesDto userDefaultFavorites = new UserDefaultFavoritesDto();
            userDefaultFavorites.UserId = CurrentUser.GetId();
            var currentTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            userDefaultFavorites.DefaultFavorites = new List<DefaultFavoriteDto>()
            {
                new()
                {
                    CoingeckoId = CommonConstant.AelfCoingeckoId,
                    Collected = true,
                    CollectTimestamp = currentTime,
                    Symbol = CommonConstant.AelfSymbol
                },
                new()
                {
                    CoingeckoId = CommonConstant.SgrCoingeckoId,
                    Collected = true,
                    CollectTimestamp = currentTime,
                    Symbol = CommonConstant.SgrSymbol
                }
            };
            var defaultTokens = await grain.UserCollectDefaultTokenAsync(userDefaultFavorites);
            if (defaultTokens is { Data: not null })
            {
                favoritesGrainDto.Favorites.AddRange(defaultTokens.Data.Favorites);
            }
        }

        var sortedCoinIds = ExtractSortedCoinIds(favoritesGrainDto);
        List<CoinMarkets> coinMarkets = null;
        foreach (var marketDataProvider in _marketDataProviders)
        {
            try
            {
                coinMarkets = await marketDataProvider.GetCoinMarketsByCoinIdsAsync(sortedCoinIds, 50);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "invoke GetHotListingsAsync after collecting error");
            }
        }

        if (coinMarkets.IsNullOrEmpty())
        {
            return result;
        }
        // user the default sort strategy, sorted by user collect time
        var coinIdToMarket = coinMarkets.ToDictionary(c => c.Id, c => c);
        
        List<MarketCryptocurrencyDto> dtos = new List<MarketCryptocurrencyDto>();
        foreach (var coinId in sortedCoinIds)
        {
            if (coinIdToMarket.TryGetValue(coinId, out var item))
            {
                dtos.Add(_objectMapper.Map<CoinMarkets, MarketCryptocurrencyDto>(item));
            }
        }
        if (!dtos.IsNullOrEmpty())
        {
            await _distributedCache.SetAsync(GetCachePrefix(MarketChosenType.Favorites) + userId, JsonConvert.SerializeObject(dtos), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_marketCacheProvider.GetExpirationMinutes())
            });
        }
        return dtos;
    }

    private string[] ExtractSortedCoinIds(UserMarketTokenFavoritesGrainDto favoritesGrainDto)
    {
        var sortedIds = new List<string>();
        sortedIds.Add(CommonConstant.AelfCoingeckoId);
        sortedIds.Add(CommonConstant.SgrCoingeckoId);
        var sortedOtherIds = favoritesGrainDto.Favorites
            .Where(f => !sortedIds.Contains(f.CoingeckoId) && f.Collected && !f.CoingeckoId.IsNullOrEmpty())
            .OrderByDescending(f => f.CollectTimestamp)
            .Select(f => f.CoingeckoId).ToList();
        sortedIds.AddRange(sortedOtherIds);
        return  sortedIds.ToArray();
    }

    private List<MarketCryptocurrencyDto> CryptocurrencyDataSortHandler(List<MarketCryptocurrencyDto> result, string sort, string sortDir)
    {
        if (result.IsNullOrEmpty() || sort.IsNullOrEmpty())
        {
            return result;
        }
        if (MarketSort.symbol.ToString().Equals(sort))
        {
            return MarketSortDir.desc.ToString().Equals(sortDir) ? result.OrderByDescending(r => r.Symbol).ToList() 
                : result.OrderBy(r => r.Symbol).ToList();
        }
        else if (MarketSort.currentPrice.ToString().Equals(sort))
        {
            return MarketSortDir.desc.ToString().Equals(sortDir) ? result.OrderByDescending(r => r.CurrentPrice).ToList() 
                : result.OrderBy(r => r.CurrentPrice).ToList();
        }
        else if (MarketSort.priceChangePercentage24H.ToString().Equals(sort))
        {
            return MarketSortDir.desc.ToString().Equals(sortDir) ? result.OrderByDescending(r => r.PriceChangePercentage24H).ToList()
                : result.OrderBy(r => r.PriceChangePercentage24H).ToList();
        }
        else
        {
            return MarketSortDir.desc.ToString().Equals(sortDir) ? result.OrderByDescending(r => r.OriginalMarketCap).ToList() 
                : result.OrderBy(r => r.OriginalMarketCap).ToList();
        }
    }

    public async Task UserCollectMarketFavoriteToken(string id, string symbol)
    {
        if (id.IsNullOrEmpty() || symbol.IsNullOrEmpty())
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            return;
        }

        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<IUserMarketTokenFavoritesGrain>(userId);
        var grainResult = await grain.GetUserTokenFavorites(userId, id, false);
        //existed 
        if (grainResult.Success)
        {
            await grain.UserReCollectFavoriteTokenAsync(userId, id);
        }
        else
        {
            await grain.UserCollectTokenAsync(new UserMarketTokenFavoritesDto()
            {
                UserId = userId,
                CoingeckoId = id,
                Collected = true,
                CollectTimestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000,
                Symbol = symbol
            });
        }
        //clear cache
        await _distributedCache.RemoveAsync(GetCachePrefix(MarketChosenType.Favorites) + userId);
    }

    public async Task UserCancelMarketFavoriteToken(string id, string symbol)
    {
        if (id.IsNullOrEmpty() || symbol.IsNullOrEmpty())
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            return;
        }
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<IUserMarketTokenFavoritesGrain>(userId);
        var grainResult = await grain.GetUserTokenFavorites(userId, id, true);
        if (!grainResult.Success)
        {
            throw new UserFriendlyException("you collect the token failed, please try again later");
        }

        await grain.UserCancelFavoriteTokenAsync(userId, id);
        //clear cache
        await _distributedCache.RemoveAsync(GetCachePrefix(MarketChosenType.Favorites) + userId);
    }
}