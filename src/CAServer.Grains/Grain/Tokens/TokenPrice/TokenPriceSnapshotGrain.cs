using CAServer.Grains.State.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens.TokenPrice;

public class TokenPriceSnapshotGrain : Grain<TokenPriceSnapshotState>, ITokenPriceSnapshotGrain
{
    private readonly ITokenPriceProvider _tokenPriceProvider;

    public TokenPriceSnapshotGrain(ITokenPriceProvider tokenPriceProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<TokenPriceGrainDto>> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        var result = new GrainResultDto<TokenPriceGrainDto>();
        if (dateTime == State.TimeStamp)
        {
            result.Success = true;
            result.Data = new TokenPriceGrainDto
            {
                Symbol = State.Symbol,
                PriceInUsd = State.PriceInUsd
            };
            return result;
        }

        var price = await _tokenPriceProvider.GetHistoryPriceAsync(symbol, dateTime);
        State.Id = this.GetPrimaryKeyString();
        State.Symbol = symbol;
        State.PriceInUsd = price;
        State.TimeStamp = dateTime;
        await WriteStateAsync();
        result.Success = true;
        result.Data = new TokenPriceGrainDto
        {
            Symbol = State.Symbol,
            PriceInUsd = State.PriceInUsd
        };
        return result;
    }
}