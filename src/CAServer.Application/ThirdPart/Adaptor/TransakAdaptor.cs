using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.Ramp;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Adaptor;

public class TransakAdaptor : IThirdPartAdaptor, ITransientDependency
{
    public string ThirdPart()
    {
        return ThirdPartNameType.Transak.ToString();
    }

    public Task<List<RampFiatItem>> GetFiatListAsync(string type, string crypto)
    {
        throw new System.NotImplementedException();
    }

    public Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }
}