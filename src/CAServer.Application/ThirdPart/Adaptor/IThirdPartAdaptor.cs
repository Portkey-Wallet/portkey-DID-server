using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using Volo.Abp;

namespace CAServer.ThirdPart.Adaptor;

public interface IThirdPartAdaptor
{

    string ThirdPart();

    Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest);

    Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampLimitRequest);

    Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampExchangeRequest);

    Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest);

    Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest);

}