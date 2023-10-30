using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart.Adaptor;

public class AlchemyAdaptor : CAServerAppService, IThirdPartAdaptor
{
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;

    public AlchemyAdaptor(AlchemyProvider alchemyProvider, IAlchemyServiceAppService alchemyServiceAppService,
        IOptionsMonitor<RampOptions> rampOptions)
    {
        _alchemyProvider = alchemyProvider;
        _alchemyServiceAppService = alchemyServiceAppService;
        _rampOptions = rampOptions;
    }


    public string ThirdPart()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    private ThirdPartProviders alchemyProviderOption()
    {
        return _rampOptions.CurrentValue.Providers[ThirdPart()];
    }

    public async Task<List<RampFiatItem>> GetFiatListAsync(string type, string crypto)
    {
        try
        {
            var alchemyFiatList = await _alchemyServiceAppService.GetAlchemyFiatListAsync(new GetAlchemyFiatListDto
            {
                Type = type
            });
            AssertHelper.IsTrue(alchemyFiatList.Success, "GetFiatListAsync error {Msg}", alchemyFiatList.Message);
            var rampFiatList = alchemyFiatList.Data.Select(f => new RampFiatItem()
            {
                Country = f.Country,
                Symbol = f.Currency,
                CountryName = f.CountryName,
                Icon = alchemyProviderOption().CountryIconUrl.ReplaceWithDict(new Dictionary<string, string>
                {
                    ["ISO"] = f.Country
                })
            }).ToList();
            return rampFiatList;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetFiatList error");
            return new List<RampFiatItem>();
        }
    }

    public Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampDetailRequest)
    {
        throw new NotImplementedException();
    }

    public Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampDetailRequest)
    {
        throw new NotImplementedException();
    }

    public Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        throw new NotImplementedException();
    }

    public Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        throw new NotImplementedException();
    }
}