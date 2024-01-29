using System;
using System.Collections.Generic;
using System.Globalization;
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
using Newtonsoft.Json;
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
            coverageDto.ThirdPart[k] = _objectMapper.Map<ThirdPartProvider, RampProviderDto>(v);
        }

        return Task.FromResult(new CommonResponseDto<RampCoverageDto>(coverageDto));
    }

    private Dictionary<string, ThirdPartProvider> GetRampProviders(string type = null)
    {
        if (_rampOptions?.CurrentValue?.Providers == null) return new Dictionary<string, ThirdPartProvider>();

        // expression params
        var getParamDict = (bool baseCoverage) => new Dictionary<string, object>
        {
            [RampConditionParams.BaseCoverage] = baseCoverage,
            [RampConditionParams.PortkeyId] = (CurrentUser.Id ?? new Guid()).ToString(),
            [RampConditionParams.PortkeyIdWhitelist] = _rampOptions.CurrentValue.PortkeyIdWhiteList,
            [RampConditionParams.DeviceType] = DeviceInfoContext.CurrentDeviceInfo?.ClientType,
            [RampConditionParams.DeviceVersion] = DeviceInfoContext.CurrentDeviceInfo?.Version,
        };

        var calculateCoverage = (string providerName, ThirdPartProvider provider) =>
        {
            var paramDict = getParamDict(provider.Coverage.OffRamp);
            // if off-ramp support
            var supportOffRamp = ExpressionHelper.Evaluate(
                _rampOptions.CurrentValue.CoverageExpressions[providerName].OffRamp,
                getParamDict(provider.Coverage.OffRamp));
            var currentDeviceInfo = DeviceInfoContext.CurrentDeviceInfo;
            var dictionary = getParamDict(provider.Coverage.OnRamp);
            // if on-ramp support
            var supportOnRamp = ExpressionHelper.Evaluate(
                _rampOptions.CurrentValue.CoverageExpressions[providerName].OnRamp,
                getParamDict(provider.Coverage.OnRamp));

            // calculate by input-type
            if (!supportOffRamp && !supportOnRamp) return null;
            if (type == OrderTransDirect.BUY.ToString() && !supportOnRamp) return null;
            if (type == OrderTransDirect.SELL.ToString() && !supportOffRamp) return null;
            var copy = JsonConvert.DeserializeObject<ThirdPartProvider>(JsonConvert.SerializeObject(provider));
            copy.Coverage.OnRamp = supportOnRamp;
            copy.Coverage.OffRamp = supportOffRamp;
            return Tuple.Create(providerName, copy);
        };

        return _rampOptions.CurrentValue.Providers
            .Select(kv => calculateCoverage(kv.Key, kv.Value))
            .Where(t => t != null && t.Item1.NotNullOrEmpty() && t.Item2 != null)
            .ToDictionary(t => t.Item1, t => t.Item2);
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
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(RampCryptoRequest request)
    {
        try
        {
            var cryptoListTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetCryptoListAsync(request)).ToList();
            var cryptoLists = (await Task.WhenAll(cryptoListTasks))
                .Select(list => list.ToDictionary(crypto => string.Join(CommonConstant.Underline, crypto.Symbol, crypto.Network)))
                .ToList();
            AssertHelper.NotEmpty(cryptoLists, "Empty crypto list");

            // get support crypto from options
            var defaultCurrencyOption = _rampOptions?.CurrentValue?.DefaultCurrency ?? new DefaultCurrencyOption();
            var cryptoDto = new RampCryptoDto
            {
                DefaultCrypto = defaultCurrencyOption.ToCrypto()
            };
            var cryptoList = _rampOptions?.CurrentValue?.CryptoList;
            for (var i = 0; cryptoList != null && i < cryptoList.Count; i++)
            {
                var crypto = cryptoList[i];
                if (!crypto.Enable)
                    continue;
                
                var cryptoKey = string.Join(CommonConstant.Underline, crypto.Symbol, crypto.Network);
                if (!cryptoLists.Any(list => list.ContainsKey(cryptoKey)))
                    continue;
                
                cryptoDto.CryptoList.Add(_objectMapper.Map<CryptoItem, RampCurrencyItem>(crypto));
            }

            return new CommonResponseDto<RampCryptoDto>(cryptoDto);
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "GetRampCryptoListAsync failed, type={Type}, fiat={Fiat}", request.Type, request.Fiat);
            return new CommonResponseDto<RampCryptoDto>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampCryptoListAsync ERROR, type={Type}, fiat={Fiat}", request.Type, request.Fiat);
            return new CommonResponseDto<RampCryptoDto>().Error(e, "Internal error, please try again later");
        }
    }

    /// <summary>
    ///     Fiat list
    /// </summary>
    /// <param name="rampFiatRequest"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(RampFiatRequest rampFiatRequest)
    {
        try
        {
            // fiat-country => item
            var fiatDict = new SortedDictionary<string, RampFiatItem>();

            // invoke provider-adaptors ASYNC
            var fiatTask = GetThirdPartAdaptors(rampFiatRequest.Type).Values
                .Select(adaptor => adaptor.GetFiatListAsync(rampFiatRequest)).ToList();

            var fiatList = (await Task.WhenAll(fiatTask))
                .Where(res => !res.IsNullOrEmpty())
                .SelectMany(list => list)
                .Where(item => item != null)
                .ToList();
            AssertHelper.NotEmpty(fiatList, "Fiat list empty");

            foreach (var fiatItem in fiatList)
            {
                var id = GrainIdHelper.GenerateGrainId(fiatItem.Symbol, fiatItem.Country);
                fiatDict.GetOrAdd(id, _ => fiatItem);
            }

            // default 
            var defaultCurrencyOption =
                (_rampOptions?.CurrentValue?.DefaultCurrency ?? new DefaultCurrencyOption()).ToFiat();
            var defaultFiatId =
                GrainIdHelper.GenerateGrainId(defaultCurrencyOption.Symbol, defaultCurrencyOption.Country);

            // ensure that default fiat in fiat-list
            var defaultFiatExists = fiatDict.TryGetValue(defaultFiatId, out var defaultFiatItem);
            defaultFiatItem = defaultFiatExists
                ? defaultFiatItem
                : fiatDict.Values.FirstOrDefault(f => f.Symbol == defaultCurrencyOption.Symbol,
                    fiatDict.Values.First());
            var defaultFiat = ObjectMapper.Map<RampFiatItem, DefaultFiatCurrency>(defaultFiatItem);
            defaultFiat.Amount = defaultCurrencyOption.Amount;

            return new CommonResponseDto<RampFiatDto>(new RampFiatDto
            {
                FiatList = fiatDict.Values.ToList(),
                DefaultFiat = defaultFiat
            });
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "GetRampFiatListAsync failed, type={Type}, fiat={Fiat}",
                rampFiatRequest.Type, rampFiatRequest.Crypto);
            return new CommonResponseDto<RampFiatDto>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetRampFiatListAsync ERROR, type={Type}, crypto={Crypto}",
                rampFiatRequest.Type, rampFiatRequest.Crypto);
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

            // invoke provider-adaptors ASYNC
            var limitTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampLimitAsync(request)).ToList();

            var limitList = (await Task.WhenAll(limitTasks)).Where(limit => limit != null).ToList();
            AssertHelper.NotEmpty(limitList, "Empty limit list");
            _logger.LogDebug("Ramp limit: {Limit}", JsonConvert.SerializeObject(limitList));

            rampLimit.Crypto = request.IsBuy()
                ? null
                : new CurrencyLimit
                {
                    Symbol = request.Crypto,
                    MinLimit = limitList.Min(limit => limit.Crypto.MinLimit.SafeToDecimal()).ToString(6),
                    MaxLimit = limitList.Max(limit => limit.Crypto.MaxLimit.SafeToDecimal()).ToString(6)
                };

            rampLimit.Fiat = request.IsSell()
                ? null
                : new CurrencyLimit
                {
                    Symbol = request.Fiat,
                    MinLimit = limitList.Min(limit => limit.Fiat.MinLimit.SafeToDecimal())
                        .ToString(CultureInfo.InvariantCulture),
                    MaxLimit = limitList.Max(limit => limit.Fiat.MaxLimit.SafeToDecimal())
                        .ToString(CultureInfo.InvariantCulture)
                };

            return new CommonResponseDto<RampLimitDto>(rampLimit);
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "GetRampLimitAsync failed, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampLimitDto>().Error(e);
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
            // invoke provider-adaptors ASYNC
            var exchangeTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampExchangeAsync(request)).ToList();

            // choose the MAX crypto-fiat exchange rate
            var maxExchanges = (await Task.WhenAll(exchangeTasks)).Where(limit => limit != null);
            var maxExchange = (request.IsBuy() ? maxExchanges.Min() : maxExchanges.Max()) ?? 0;
            AssertHelper.IsTrue(maxExchange > 0, "empty maxExchange");

            var exchange = new RampExchangeDto
            {
                Exchange = maxExchange.ToString(8)
            };
            return new CommonResponseDto<RampExchangeDto>(exchange);
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning(e, "GetRampExchangeAsync failed, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampExchangeDto>().Error(e);
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
            AssertHelper.IsTrue(!request.IsBuy() || (request.FiatAmount ?? 0) > 0,
                "Invalid fiat amount");
            AssertHelper.IsTrue(!request.IsSell() || (request.CryptoAmount ?? 0) > 0,
                "Invalid crypto amount");

            // invoke provider-adaptors ASYNC
            var priceTask = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampPriceAsync(request)).ToList();

            // order by price and choose the MAX one
            var priceList = (await Task.WhenAll(priceTask)).Where(price => price != null)
                .OrderByDescending(price => request.IsBuy()
                    ? price.CryptoAmount.SafeToDouble()
                    : price.FiatAmount.SafeToDouble())
                .ToList();
            AssertHelper.NotEmpty(priceList, "Price list empty");

            _logger.LogDebug("Ramp price: {Price}", JsonConvert.SerializeObject(priceList));
            return new CommonResponseDto<RampPriceDto>(priceList.First());
        }
        catch (UserFriendlyException e)
        {
            Logger.LogError(e, "GetRampPriceAsync failed, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampPriceDto>().Error(e);
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

            // invoke provider-adaptors ASYNC
            var detailTasks = GetThirdPartAdaptors(request.Type).Values
                .Select(adaptor => adaptor.GetRampDetailAsync(request))
                .ToList();

            var detailList = (await Task.WhenAll(detailTasks)).Where(detail => detail != null)
                .OrderByDescending(detail => request.IsBuy() ? detail.CryptoAmount : detail.FiatAmount)
                .ToList();
            AssertHelper.NotEmpty(detailList, "Ramp detail list empty");

            _logger.LogDebug("Ramp detail: {Detail}", JsonConvert.SerializeObject(detailList));
            foreach (var providerRampDetailDto in detailList)
            {
                providerRampDetailDto.FiatAmount = request.IsBuy() ? null : providerRampDetailDto.FiatAmount;
                providerRampDetailDto.CryptoAmount = request.IsSell() ? null : providerRampDetailDto.CryptoAmount;
            }

            return new CommonResponseDto<RampDetailDto>(new RampDetailDto
            {
                ProvidersList = detailList
            });
        }
        catch (UserFriendlyException e)
        {
            Logger.LogError(e, "GetRampDetailAsync failed, crypto={Crypto}, fiat={Fiat}", request.Crypto,
                request.Fiat);
            return new CommonResponseDto<RampDetailDto>().Error(e);
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
}