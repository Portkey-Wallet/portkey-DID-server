using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Tokens.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<Dictionary<string, decimal>> GetPriceAsync(params string[] symbols);

    Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime);
    int GetPriority();
}