using CAServer.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public interface ITokenPriceGrain : IGrainWithStringKey
{
    Task<TokenPriceDataDto> GetCurrentPriceAsync(string symbol);
}