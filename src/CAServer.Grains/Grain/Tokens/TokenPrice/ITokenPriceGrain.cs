using Orleans;

namespace CAServer.Grains.Grain.Tokens.TokenPrice;

public interface ITokenPriceGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenPriceGrainDto>> GetCurrentPriceAsync(string symbol);
}