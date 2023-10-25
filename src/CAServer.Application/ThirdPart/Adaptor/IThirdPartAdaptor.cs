using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using Volo.Abp;

namespace CAServer.ThirdPart.Adaptor;

public interface IThirdPartAdaptor
{

    string ThirdPart();

    Task<List<RampFiatItem>> GetFiatListAsync(string type, string crypto);

    Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampDetailRequest);

    Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampDetailRequest);

    Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest);

    Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest);

    Task<RampFreeLoginDto> GetRampFreeLoginAsync(RampFreeLoginRequest input)
    {
        throw new UserFriendlyException("Not support action");
    }

    Task<AlchemySignatureResultDto> GetRampThirdPartSignatureAsync(RampSignatureRequest input)
    {
        throw new UserFriendlyException("Not support action");
    }

}