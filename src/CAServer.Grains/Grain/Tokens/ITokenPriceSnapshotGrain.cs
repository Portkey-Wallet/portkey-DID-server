using CAServer.Tokens;
using Orleans;

namespace CAServer.Grains.Grain.Tokens;

public interface ITokenPriceSnapshotGrain : IGrainWithStringKey
{
    Task<TokenPriceDataDto> GetHistoryPriceAsync(string symbol, DateTime dateTime);

}