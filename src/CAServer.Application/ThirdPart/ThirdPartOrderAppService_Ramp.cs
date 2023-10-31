using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Options;
using CAServer.ThirdPart.Adaptor;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Etos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppService
{
    /// <summary>
    ///     Ramp coverage
    /// </summary>
    /// <returns></returns>
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

    private Dictionary<string, ThirdPartProviders> GetRampProviders(string type = null)
    {
        // expression params
        var getParamDict = (bool baseCoverage) => new Dictionary<string, object>
        {
            ["baseCoverage"] = baseCoverage,
            ["portkeyId"] = (CurrentUser.Id ?? new Guid()).ToString(),
            ["portkeyIdWhitelist"] = _rampOptions.CurrentValue.PortkeyIdWhiteList,
            ["deviceType"] = "WebSDK" // TODO nzc
        };

        var calculateCoverage = (string providerName, ThirdPartProviders provider) =>
        {
            // if off-ramp support
            provider.Coverage.OffRamp = ExpressionHelper.Evaluate<bool>(
                _rampOptions.CurrentValue.CoverageExpressions[providerName].OffRamp,
                getParamDict(provider.Coverage.OffRamp));

            // if on-ramp support
            provider.Coverage.OnRamp = ExpressionHelper.Evaluate<bool>(
                _rampOptions.CurrentValue.CoverageExpressions[providerName].OnRamp,
                getParamDict(provider.Coverage.OnRamp));

            // calculate by input-type
            if (!provider.Coverage.OffRamp && !provider.Coverage.OnRamp) return null;
            if (type == OrderTransDirect.BUY.ToString() && !provider.Coverage.OnRamp) return null;
            if (type == OrderTransDirect.SELL.ToString() && !provider.Coverage.OffRamp) return null;
            return provider;
        };

        return _rampOptions?.CurrentValue?.Providers == null
            ? new Dictionary<string, ThirdPartProviders>()
            : _rampOptions.CurrentValue.Providers
                .Where(p => calculateCoverage(p.Key, p.Value) != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private Dictionary<string, IThirdPartAdaptor> GetThirdPartAdaptors(string type = null)
    {
        var providers = GetRampProviders(type);
        return providers.IsNullOrEmpty()
            ? new Dictionary<string, IThirdPartAdaptor>()
            : _thirdPartAdaptors
                .Where(a => providers.ContainsKey(a.Key))
                .ToDictionary(a => a.Key, a => a.Value);
    }

    /// <summary>
    ///     Crypto list
    /// </summary>
    /// <param name="type"></param>
    /// <param name="fiat"></param>
    /// <returns></returns>
    public Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(string type, string fiat)
    {
        try
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
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampCryptoListAsync ERROR, type={Type}, fiat={Fiat}", type, fiat);
            return Task.FromResult(
                new CommonResponseDto<RampCryptoDto>().Error(e, "Internal error, please try again later"));
        }
    }

    /// <summary>
    ///     Fiat list
    /// </summary>
    /// <param name="type"></param>
    /// <param name="crypto"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(string type, string crypto)
    {
        try
        {
            // fiat-country => item
            var fiatDict = new SortedDictionary<string, RampFiatItem>();

            // invoke adaptors ASYNC
            var fiatTask = GetThirdPartAdaptors(type).Values
                .Select(adaptor => adaptor.GetFiatListAsync(type, crypto)).ToList();

            var fiatList = (await Task.WhenAll(fiatTask))
                .Where(res => res != null)
                .SelectMany(list => list).ToList();
            AssertHelper.NotEmpty(fiatList, "Fiat list empty");
            foreach (var fiatItem in fiatList)
            {
                var id = GrainIdHelper.GenerateGrainId(fiatItem.Symbol, fiatItem.Country);
                fiatDict.GetOrAdd(id, _ => fiatItem);
            }

            var defaultCurrencyOption = _rampOptions?.CurrentValue?.DefaultCurrency ?? new DefaultCurrencyOption();
            return new CommonResponseDto<RampFiatDto>(new RampFiatDto
            {
                FiatList = fiatDict.Values.ToList(),
                DefaultFiat = defaultCurrencyOption.ToFiat()
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampFiatListAsync ERROR, type={Type}, crypto={Crypto}", type, crypto);
            return new CommonResponseDto<RampFiatDto>().Error(e, "Internal error, please try again later");
        }
    }


    /// <summary>
    ///     Ramp limit
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampLimitDto>> GetRampLimitAsync(RampLimitRequest request)
    {
        try
        {
            var rampLimit = new RampLimitDto();

            // invoke adaptors ASYNC
            var limitTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampLimitAsync(request)).ToList();

            var limitList = (await Task.WhenAll(limitTasks)).Where(limit => limit != null).ToList();
            AssertHelper.NotEmpty(limitList, "Empty limit list");

            rampLimit.Crypto = new CurrencyLimit
            {
                Symbol = request.Crypto,
                MinLimit = limitList.Select(limit => limit.Crypto.MinLimit).Min(),
                MaxLimit = limitList.Select(limit => limit.Crypto.MaxLimit).Max()
            };

            rampLimit.Fiat = new CurrencyLimit
            {
                Symbol = request.Fiat,
                MinLimit = limitList.Select(limit => limit.Fiat.MinLimit).Min(),
                MaxLimit = limitList.Select(limit => limit.Fiat.MaxLimit).Max()
            };
            return new CommonResponseDto<RampLimitDto>(rampLimit);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampLimitAsync ERROR, crypto={Crypto}, fiat={Fiat}", request.Crypto, request.Fiat);
            return new CommonResponseDto<RampLimitDto>().Error(e, "Internal error, please try again later");
        }
    }

    /// <summary>
    ///     Ramp exchange
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampExchangeDto>> GetRampExchangeAsync(RampExchangeRequest request)
    {
        try
        {
            // invoke adaptors ASYNC
            var exchangeTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampExchangeAsync(request)).ToList();

            // choose the MAX crypto-fiat exchange rate
            var maxExchange = (await Task.WhenAll(exchangeTasks)).Where(limit => limit != null).Max();
            AssertHelper.NotNull(maxExchange, "empty maxExchange");

            var exchange = new RampExchangeDto
            {
                Exchange = maxExchange.ToString()
            };
            return new CommonResponseDto<RampExchangeDto>(exchange);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampExchangeAsync ERROR, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampExchangeDto>().Error(e, "Internal error, please try again later");
        }
    }

    /// <summary>
    ///     Calculate Ramp price
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request)
    {
        try
        {
            AssertHelper.IsTrue(request.IsBuy() && request.FiatAmount != null && request.FiatAmount > 0,
                "Invalid fiat amount");
            AssertHelper.IsTrue(request.IsSell() && request.CryptoAmount != null && request.CryptoAmount > 0,
                "Invalid crypto amount");

            // invoke adaptors ASYNC
            var priceTask = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampPriceAsync(request)).ToList();

            // order by price and choose the MAX one
            var priceList = (await Task.WhenAll(priceTask)).Where(price => price != null)
                .OrderBy(price => request.IsBuy()
                    ? price.CryptoAmount.SafeToDouble()
                    : price.FiatAmount.SafeToDouble())
                .ToList();
            AssertHelper.NotEmpty(priceTask, "Price list empty");

            return new CommonResponseDto<RampPriceDto>(priceList.First());
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampPriceAsync ERROR, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampPriceDto>().Error(e, "Internal error, please try again later");
        }
    }

    /// <summary>
    ///     Ramp detail
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request)
    {
        try
        {
            AssertHelper.NotEmpty(request.Crypto, "Param crypto empty");
            AssertHelper.NotEmpty(request.Fiat, "Param fiat empty");

            // invoke adaptors ASYNC
            var detailTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampDetailAsync(request))
                .ToList();
            var detailList = (await Task.WhenAll(detailTasks)).Where(detail => detail != null).ToList();
            AssertHelper.NotEmpty(detailList, "Ramp detail list empty");

            return new CommonResponseDto<RampDetailDto>(new RampDetailDto
            {
                ProvidersList = detailList
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampDetailAsync ERROR, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampDetailDto>().Error(e, "Internal error, please try again later");
        }
    }

    /// <summary>
    ///     Off-ramp: send transfer transaction forward to Node
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<CommonResponseDto<Empty>> TransactionForwardCallAsync(TransactionDto input)
    {
        try
        {
            _logger.LogInformation("TransactionAsync start, OrderId: {orderId}", input.OrderId);
            if (!VerifyInput(input))
            {
                _logger.LogWarning("Transaction input valid failed, orderId:{orderId}", input.OrderId);
                await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto
                {
                    OrderId = input.OrderId.ToString(),
                    RawTransaction = input.RawTransaction,
                    Status = OrderStatusType.Invalid,
                    DicExt = new Dictionary<string, object>()
                    {
                        ["reason"] = "Transaction input valid failed."
                    }
                });
                throw new UserFriendlyException("Input validation failed.");
            }

            var transactionEto = ObjectMapper.Map<TransactionDto, TransactionEto>(input);
            await _distributedEventBus.PublishAsync(transactionEto);
            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "Transaction forward call failed");
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Transaction forward call error");
            return new CommonResponseDto<Empty>().Error(e, "Internal error please try again later.");
        }
    }

    
    private bool VerifyInput(TransactionDto input)
    {
        try
        {
            var validStr = EncryptionHelper.MD5Encrypt32(input.OrderId + input.RawTransaction);
            var publicKey = ByteArrayHelper.HexStringToByteArray(input.PublicKey);
            var signature = ByteArrayHelper.HexStringToByteArray(input.Signature);
            var data = Encoding.UTF8.GetBytes(validStr).ComputeHash();
            return CryptoHelper.VerifySignature(signature, data, publicKey);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Input validation internal error");
            return false;
        }
    }
    
    public async Task<CommonResponseDto<RampFreeLoginDto>> GetRampThirdPartFreeLoginTokenAsync(
        RampFreeLoginRequest input)
    {
        try
        {
            AssertHelper.NotEmpty(input.ThirdPart, "param thirdPart empty");

            var adaptor = GetThirdPartAdaptors()[input.ThirdPart];
            AssertHelper.NotNull(adaptor, "Provider {ThirdPart} not found", input.ThirdPart);

            return new CommonResponseDto<RampFreeLoginDto>(await adaptor.GetRampFreeLoginAsync(input));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampThirdPartFreeLoginTokenAsync ERROR, thirdPart={ThirdPart}", input.ThirdPart);
            return new CommonResponseDto<RampFreeLoginDto>().Error(e, "Internal error, please try again later");
        }
    }

    public async Task<CommonResponseDto<AlchemySignatureResultDto>> GetRampThirdPartSignatureAsync(
        RampSignatureRequest input)
    {
        try
        {
            AssertHelper.NotEmpty(input.ThirdPart, "param thirdPart empty");

            var adaptor = GetThirdPartAdaptors()[input.ThirdPart];
            AssertHelper.NotNull(adaptor, "Provider {ThirdPart} not found", input.ThirdPart);

            return new CommonResponseDto<AlchemySignatureResultDto>(
                await adaptor.GetRampThirdPartSignatureAsync(input));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampThirdPartSignatureAsync ERROR, thirdPart={ThirdPart}", input.ThirdPart);
            return new CommonResponseDto<AlchemySignatureResultDto>().Error(e,
                "Internal error, please try again later");
        }
    }
}