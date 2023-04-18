using CAServer.Grains.State.Tokens;
using CAServer.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public class TokenPriceGrain : Grain<CurrentTokenPriceState>,ITokenPriceGrain
{
    private readonly ITokenPriceProvider _tokenPriceProvider;

    public TokenPriceGrain(ITokenPriceProvider tokenPriceProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
    }
    
    public async Task<TokenPriceDataDto> GetCurrentPriceAsync(string symbol)
    {
        await ReadStateAsync();
        if (State.PriceUpdateTime.AddMinutes(15) > DateTime.UtcNow)
        {
            return new TokenPriceDataDto
            {
                Symbol = State.Symbol,
                PriceInUsd = State.PriceInUsd
            };
        }

        var price = await _tokenPriceProvider.GetPriceAsync(symbol);
        State.Id = this.GetPrimaryKeyString();
        State.Symbol = symbol;
        State.PriceInUsd = price;
        State.PriceUpdateTime = DateTime.UtcNow;
        await WriteStateAsync();

        return new TokenPriceDataDto
        {
            Symbol = symbol,
            PriceInUsd = price
        };
    }
}