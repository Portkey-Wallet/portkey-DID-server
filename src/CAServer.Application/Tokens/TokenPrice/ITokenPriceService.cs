using System.Threading.Tasks;
using CAServer.Tokens.Dtos;

namespace CAServer.Tokens.TokenPrice;

public interface ITokenPriceService
{
    /// <summary>
    /// Retrieve token price from Redis cache
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    Task<TokenPriceDataDto> GetCurrentPriceAsync(string symbol);

    /// <summary>
    /// Retrieve token historical prices from Redis cache
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    Task<TokenPriceDataDto> GetHistoryPriceAsync(string symbol, string dateTime);

    /// <summary>
    /// Refresh the token price in the Redis cache.
    /// </summary>
    /// <param name="symbol">Symbol == null, refresh all tokens.</param>
    /// <returns></returns>
    Task RefreshCurrentPriceAsync(string symbol = default);
}