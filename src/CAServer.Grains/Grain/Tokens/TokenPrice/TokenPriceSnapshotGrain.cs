using CAServer.Grains.State.Tokens;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAServer.Grains.Grain.Tokens.TokenPrice;

public class TokenPriceSnapshotGrain : Grain<TokenPriceSnapshotState>, ITokenPriceSnapshotGrain
{
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly ILogger<TokenPriceSnapshotGrain> _logger;

    public TokenPriceSnapshotGrain(ITokenPriceProvider tokenPriceProvider, ILogger<TokenPriceSnapshotGrain> logger)
    {
        _tokenPriceProvider = tokenPriceProvider;
        _logger = logger;
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
        if (dateTime == State.TimeStamp && State.PriceInUsd != 0)
        {
            result.Success = true;
            result.Data = new TokenPriceGrainDto
            {
                Symbol = State.Symbol,
                PriceInUsd = State.PriceInUsd
            };
            return result;
        }

        decimal price = 0;
        try
        {
            price = await _tokenPriceProvider.GetHistoryPriceAsync(symbol, dateTime);
            if (price == 0)
            {
                result.Success = true;
                result.Data = new TokenPriceGrainDto
                {
                    Symbol = symbol,
                    PriceInUsd = State.PriceInUsd
                };
                return result;
            }
            State.Id = this.GetPrimaryKeyString();
            State.Symbol = symbol;
            State.PriceInUsd = price;
            State.TimeStamp = dateTime;
            await WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get history price error: {symbol}, {dateTime}", symbol, dateTime);
        }

        result.Success = true;
        result.Data = new TokenPriceGrainDto
        {
            Symbol = symbol,
            PriceInUsd = price
        };
        
        return result;
    }
}