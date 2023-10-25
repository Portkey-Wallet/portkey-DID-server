using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.Ramp;

namespace CAServer.ThirdPart.Adaptor;

public interface IThirdPartAdaptor
{

    string ThirdPart();

    Task<List<RampFiatItem>> GetFiatListAsync(string type, string crypto);

    Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest);

    Task<RampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest);

}