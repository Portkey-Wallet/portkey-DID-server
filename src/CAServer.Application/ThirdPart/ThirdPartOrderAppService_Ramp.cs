using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Options;
using CAServer.ThirdPart.Adaptor;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppService
{
    // GetRampCoverageAsync
    public Task<CommonResponseDto<RampCoverageDto>> GetRampCoverageAsync()
    {
        var coverageDto = new RampCoverageDto();
        var providers = GetRampProviders();
        foreach (var (k, v) in providers)
        {
            coverageDto.ThirdPart[k] = _objectMapper.Map<ThirdPartProviders, RampProviderDto>(v);
        }

        return Task.FromResult(new CommonResponseDto<RampCoverageDto>(coverageDto));
    }

    private Dictionary<string, ThirdPartProviders> GetRampProviders()
    {
        //TODO nzc choose strategy
        return _rampOptions?.CurrentValue?.Providers == null
            ? new Dictionary<string, ThirdPartProviders>()
            : _rampOptions.CurrentValue.Providers.Where(p => p.Value.Coverage.OnRamp && p.Value.Coverage.OffRamp)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private Dictionary<string, IThirdPartAdaptor> GetThirdPartAdaptors()
    {
        var providers = GetRampProviders();
        return _thirdPartAdaptors
            .Where(a => providers.ContainsKey(a.Key))
            .ToDictionary(a => a.Key, a => a.Value);
    }

    // GetRampCryptoListAsync
    public Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(string type, string fiat)
    {
        var defaultCurrencyOption = _rampOptions?.CurrentValue?.DefaultCurrency ?? new DefaultCurrencyOption();
        var cryptoDto = new RampCryptoDto
        {
            DefaultCrypto = defaultCurrencyOption.ToCrypto()
        };
        var cryptoList = _rampOptions?.CurrentValue?.CryptoList;
        for (var i = 0; cryptoList != null && i < cryptoList.Count; i++)
        {
            cryptoDto.CryptoList.Add(_objectMapper.Map<CryptoItem, RampCurrencyItem>(cryptoList[i]));
        }

        return Task.FromResult(new CommonResponseDto<RampCryptoDto>(cryptoDto));
    }

    // GetRampFiatListAsync
    public async Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(string type, string crypto)
    {
        // fiat-country => item
        var fiatDict = new SortedDictionary<string, RampFiatItem>();
        foreach (var (_, adaptor) in GetThirdPartAdaptors())
        {
            var fiatList = await adaptor.GetFiatListAsync(type, crypto);
            foreach (var fiatItem in fiatList)
            {
                var id = GrainIdHelper.GenerateGrainId(fiatItem.Symbol, fiatItem.Country);
                fiatDict.GetOrAdd(id, _ => fiatItem);
            }
        }

        var defaultCurrencyOption = _rampOptions?.CurrentValue?.DefaultCurrency ?? new DefaultCurrencyOption();
        return new CommonResponseDto<RampFiatDto>(new RampFiatDto
        {
            FiatList = fiatDict.Values.ToList(),
            DefaultFiat = defaultCurrencyOption.ToFiat()
        });
    }

    public Task<CommonResponseDto<RampLimitDto>> GetRampLimitAsync(RampLimitRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampExchangeDto>> GetRampExchangeAsync(RampExchangeRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<CommonResponseDto<Empty>> TransactionForwardCallAsync(TransactionDto input)
    {
        throw new NotImplementedException();
    }

    public async Task<CommonResponseDto<RampFreeLoginDto>> GetRampThirdPartFreeLoginTokenAsync(
        RampFreeLoginRequest input)
    {
        throw new NotImplementedException();
    }

    public async Task<CommonResponseDto<AlchemySignatureResultDto>> GetRampThirdPartSignatureAsync(
        RampSignatureRequest input)
    {
        throw new NotImplementedException();
    }
}