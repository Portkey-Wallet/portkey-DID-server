using Orleans;

namespace CAServer.Grains.Grain.Tokens.TokenPrice;

public interface ITokenPriceSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenPriceGrainDto>> GetHistoryPriceAsync(string symbol, string dateTime);

}