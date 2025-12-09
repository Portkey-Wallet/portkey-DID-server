using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Volo.Abp;

namespace CAServer.ThirdPart.Adaptor;

public interface IThirdPartAdaptor
{

    string ThirdPart();

    Task<List<RampCurrencyItem>> GetCryptoListAsync(RampCryptoRequest request);

    Task<List<TransakCryptoItem>> GetCryptoCurrenciesAsync();
    
    Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest);

    Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampLimitRequest);

    Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampExchangeRequest);

    Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest);

    Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest);

}