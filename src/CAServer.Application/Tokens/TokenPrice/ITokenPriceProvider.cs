using System.Collections.Generic;
using System.Threading.Tasks;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Search;

namespace CAServer.Tokens.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<Dictionary<string, decimal>> GetPriceAsync(params string[] symbols);

    Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime);
    bool IsAvailable();
    int GetPriority();
    
    Task<List<CoinMarkets>> GetHotListingsAsync();

    Task<List<CoinMarkets>> GetCoinMarketsByCoinIdsAsync(string[] ids, int perPage);
    
    Task<TrendingList> GetTrendingListingsAsync();
}