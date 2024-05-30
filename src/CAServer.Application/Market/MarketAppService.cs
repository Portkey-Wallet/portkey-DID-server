using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.Market;
using CAServer.Grains.State.Market;
using CAServer.Market.enums;
using CAServer.Tokens.TokenPrice;
using CAServer.Transfer;
using CAServer.Transfer.Dtos;
using CoinGecko.Entities.Response.Coins;
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

    public MarketAppService(IObjectMapper objectMapper,
        IClusterClient clusterClient, IEnumerable<ITokenPriceProvider> marketDataProviders,
        IDistributedCache<string> distributedCache, ILogger<MarketAppService> logger,
        ITransferAppService transferAppService)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _marketDataProviders = marketDataProviders;
        _distributedCache = distributedCache;
        _logger = logger;
        _transferAppService = transferAppService;
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
        else if (MarketChosenType.Favorites.ToString().Equals(type))
        {
            result = await GetFavoritesList(sort);
        }
        else
        {
            return result;
        }

        //deal with the collected logic
        await CollectedStatusHandler(result);

        //deal with the sort strategy 
        if (!"Favorites".Equals(type) || !sort.IsNullOrEmpty())
        {
            result = CryptocurrencyDataSortHandler(result, sort, sortDir);
        }

        //invoke etransfer for the SupportEtransfer field
        try
        {
            var responseFromEtransfer = await _transferAppService.GetTokenOptionListAsync(new GetTokenOptionListRequestDto()
            {
                Type = "Deposit"
            });
            _logger.LogInformation("============responseFromEtransfer={0}", JsonConvert.SerializeObject(responseFromEtransfer.Data));
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
        return result;
    }

    private async Task CollectedStatusHandler(List<MarketCryptocurrencyDto> result)
    {
        if (result.IsNullOrEmpty())
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
        foreach (var marketDataProvider in _marketDataProviders)
        {
            try
            {
                List<CoinMarkets> markets = await marketDataProvider.GetHotListingsAsync();
                if (!markets.IsNullOrEmpty())
                {
                    //todo add cache _distributedCache
                    return _objectMapper.Map<List<CoinMarkets>, List<MarketCryptocurrencyDto>>(markets);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "invoke GetHotListingsAsync error");
            }
        }

        return new List<MarketCryptocurrencyDto>();
    }

    private async Task<List<MarketCryptocurrencyDto>> GetTrendingList()
    {
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
        return _objectMapper.Map<List<CoinMarkets>, List<MarketCryptocurrencyDto>>(coinMarkets);
    }

    private async Task<List<MarketCryptocurrencyDto>> GetFavoritesList(string sort)
    {
        var userId = CurrentUser.GetId();
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
        if (grainResultDto.Data.Favorites.IsNullOrEmpty())
        {
            UserDefaultFavoritesDto userDefaultFavorites = new UserDefaultFavoritesDto();
            userDefaultFavorites.UserId = CurrentUser.GetId();
            var currentTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            userDefaultFavorites.DefaultFavorites = new List<DefaultFavoriteDto>()
            {
                new DefaultFavoriteDto()
                {
                    CoingeckoId = "aelf",
                    Collected = true,
                    CollectTimestamp = currentTime,
                    Symbol = "ELF"
                },
                new DefaultFavoriteDto()
                {
                    CoingeckoId = "schrodinger-2",
                    Collected = true,
                    CollectTimestamp = currentTime,
                    Symbol = "SGR"
                }
            };
            var defaultTokens = await grain.UserCollectDefaultTokenAsync(userDefaultFavorites);
            if (defaultTokens != null && defaultTokens.Data != null)
            {
                favoritesGrainDto.Favorites.AddRange(defaultTokens.Data.Favorites);
            }
        }
        var sortedCoinIds = favoritesGrainDto.Favorites
            .Where(f => f.Collected && !f.CoingeckoId.IsNullOrEmpty())
            .OrderByDescending(f => f.CollectTimestamp)
            .Select(f => f.CoingeckoId).ToArray();
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
        if (sort.IsNullOrEmpty())
        {
            var coinIdToMarket = coinMarkets.ToDictionary(c => c.Id, c => c);
            List<MarketCryptocurrencyDto> dtos = new List<MarketCryptocurrencyDto>();
            foreach (var coinId in sortedCoinIds)
            {
                if (coinIdToMarket.TryGetValue(coinId, out var item))
                {
                    dtos.Add(_objectMapper.Map<CoinMarkets, MarketCryptocurrencyDto>(item));
                }
            }
            return dtos;
        }
        else
        {
            return _objectMapper.Map<List<CoinMarkets>, List<MarketCryptocurrencyDto>>(coinMarkets);
        }
    }

    private List<MarketCryptocurrencyDto> CryptocurrencyDataSortHandler(List<MarketCryptocurrencyDto> result, string sort, string sortDir)
    {
        if (result.IsNullOrEmpty())
        {
            return result;
        }
        if (sort.IsNullOrEmpty())
        {
            return result;
        }
        if (sort.Equals("symbol"))
        {
            if (sortDir.Equals("desc"))
            {
                return result.OrderByDescending(r => r.Symbol).ToList();
            }
            else
            {
                return result.OrderBy(r => r.Symbol).ToList();
            }
        }
        else if (sort.Equals("currentPrice"))
        {
            if (sortDir.Equals("desc"))
            {
                return result.OrderByDescending(r => r.CurrentPrice).ToList();
            }
            else
            {
                return result.OrderBy(r => r.CurrentPrice).ToList();
            }
        }
        else if (sort.Equals("priceChangePercentage24H"))
        {
            if (sortDir.Equals("desc"))
            {
                return result.OrderByDescending(r => r.PriceChangePercentage24H).ToList();
            }
            else
            {
                return result.OrderBy(r => r.PriceChangePercentage24H).ToList();
            }
        }
        else
        {
            if (sortDir.Equals("desc"))
            {
                return result.OrderByDescending(r => r.OriginalMarketCap).ToList();
            }
            else
            {
                return result.OrderBy(r => r.OriginalMarketCap).ToList();
            }
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
        if (grainResult.Success)
        {
            return;
        }

        await grain.UserCancelFavoriteTokenAsync(userId, id);
    }
}