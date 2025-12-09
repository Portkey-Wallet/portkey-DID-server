using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Market;

public interface IMarketAppService
{
    public Task<List<MarketCryptocurrencyDto>> GetMarketCryptocurrencyDataByType(string type, string sort, string sortDir);

    public Task UserCollectMarketFavoriteToken(string id, string symbol);
    
    public Task UserCancelMarketFavoriteToken(string id, string symbol);
}