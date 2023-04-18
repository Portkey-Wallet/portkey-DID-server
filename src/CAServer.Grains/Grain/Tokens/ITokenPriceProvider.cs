using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<decimal> GetHistoryPriceAsync(string symbol, DateTime dateTime);
}