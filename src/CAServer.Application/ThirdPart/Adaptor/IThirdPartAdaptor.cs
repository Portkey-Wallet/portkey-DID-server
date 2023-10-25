using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.Ramp;

namespace CAServer.ThirdPart.Adaptor;

public interface IThirdPartAdaptor
{

    string ThirdPart();

    Task<List<RampFiatItem>> GetFiatListAsync(string type, string crypto);

    Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampDetailRequest);

    Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampDetailRequest);

    Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest);

    Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest);

}