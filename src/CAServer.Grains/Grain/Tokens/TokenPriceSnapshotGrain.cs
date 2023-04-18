using CAServer.Grains.State.Tokens;
using CAServer.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public class TokenPriceSnapshotGrain : Grain<TokenPriceSnapshotState>,ITokenPriceSnapshotGrain
{
    private readonly ITokenPriceProvider _tokenPriceProvider;

    public TokenPriceSnapshotGrain(ITokenPriceProvider tokenPriceProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
    }
    
    public async Task<TokenPriceDataDto> GetHistoryPriceAsync(string symbol, DateTime dateTime)
    {
        if (dateTime == State.TimeStamp)
        {
            return new TokenPriceDataDto
            {
                Symbol = State.Symbol,
                PriceInUsd = State.PriceInUsd
            };
        }
        var price = await _tokenPriceProvider.GetHistoryPriceAsync(symbol,dateTime);
        State.Id = this.GetPrimaryKeyString();
        State.PriceInUsd = price;
        State.TimeStamp = dateTime;
        await WriteStateAsync();
        return new TokenPriceDataDto
        {
            Symbol = State.Symbol,
            PriceInUsd = price
        };
    }
}