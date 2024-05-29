using System.Collections.Generic;
using System.Threading.Tasks;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Search;

namespace CAServer.Market;

public interface IMarketDataProvider
{
    Task<List<CoinMarkets>> GetHotListingsAsync();

    Task<List<CoinMarkets>> GetCoinMarketsByCoinIdsAsync(string[] ids, int perPage);
    
    Task<TrendingList> GetTrendingListingsAsync();
}