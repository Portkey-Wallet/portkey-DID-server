using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using CAServer.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractTreasuryProcessor : IThirdPartTreasuryProcessor
{
    private readonly ITokenAppService _tokenAppService;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public AbstractTreasuryProcessor(ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IClusterClient clusterClient, IObjectMapper objectMapper, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IDistributedEventBus distributedEventBus)
    {
        _tokenAppService = tokenAppService;
        _chainOptions = chainOptions;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _thirdPartOptions = thirdPartOptions;
        _distributedEventBus = distributedEventBus;
    }

    internal abstract Task<string> AdaptPriceInputAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext;

    internal abstract Task<TreasuryBaseResult> AdaptPriceOutputAsync(TreasuryPriceDto treasuryPriceDto);

    internal abstract Task<TreasuryOrderRequest> AdaptOrderInputAsync<TOrderInput>(TOrderInput orderInput)
        where TOrderInput : TreasuryBaseContext;


    /// <summary>
    ///     ThirdPart provider name enum
    /// </summary>
    /// <returns></returns>
    public abstract ThirdPartNameType ThirdPartName();


    /// <summary>
    ///     query treasury price
    /// </summary>
    /// <param name="priceInput"></param>
    /// <typeparam name="TPriceInput"></typeparam>
    /// <typeparam name="TPriceOutput"></typeparam>
    /// <returns></returns>
    public async Task<TreasuryBaseResult> GetPriceAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext
    {
        // Adapt the query input parameters of different suppliers.
        var cryptoSymbol = await AdaptPriceInputAsync(priceInput);

        var cryptoUsdtEx = await _tokenAppService.GetAvgLatestExchangeAsync(cryptoSymbol, CommonConstant.USDT);
        var cryptoPrice = new TreasuryPriceDto
        {
            Crypto = cryptoSymbol,
            PriceSymbol = CommonConstant.USDT,
            Price = cryptoUsdtEx.Exchange,
            NetworkFee = new Dictionary<string, FeeItem>
            {
                [CommonConstant.MainChainId] = NetworkFeeInElf(CommonConstant.MainChainId)
            }
        };

        // Adapt the returned results of different suppliers.
        return await AdaptPriceOutputAsync(cryptoPrice);
    }

    public FeeItem NetworkFeeInElf(string chainId)
    {
        var chainExists =
            _chainOptions.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo);
        AssertHelper.IsTrue(chainExists, "ChainInfo of {} not found", CommonConstant.MainChainId);
        AssertHelper.NotNull(chainInfo, "ChainInfo of {} empty", CommonConstant.MainChainId);
        return FeeItem.Crypto(CommonConstant.ELF, chainInfo!.TransactionFee.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     For thirdPart notify treasury order status
    /// </summary>
    /// <param name="thirdPartOrderInput"></param>
    /// <typeparam name="TOrderInput"></typeparam>
    /// <returns></returns>
    public async Task NotifyOrder<TOrderInput>(TOrderInput thirdPartOrderInput) where TOrderInput : TreasuryBaseContext
    {
        var orderInput = await AdaptOrderInputAsync(thirdPartOrderInput);

        var orderId = GuidHelper.UniqId(orderInput.ThirdPartName, orderInput.ThirdPartOrderId);
        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderId);
        var orderResp = await treasuryOrderGrain.GetAsync();
        AssertHelper.NotNull(orderResp, "Get order grain failed");

        var orderDto = orderResp.Data;
        AssertHelper.NotNull(orderDto, "Get order grain data failed");
        AssertHelper.IsTrue(orderDto.ThirdPartOrderId.IsNullOrEmpty(), "Order {} of {} exists {}",
            orderInput.ThirdPartOrderId, orderInput.ThirdPartName, orderId);

        var token = await _tokenAppService.GetTokenInfoAsync(CommonConstant.MainChainId, orderInput.Crypto);
        AssertHelper.NotNull(token, "Token {} not found", orderInput.Crypto);

        // token price 
        var exchange = await _tokenAppService.GetAvgLatestExchangeAsync(orderInput.Crypto, CommonConstant.USDT);
        var inputExchange = orderInput.CryptoPrice.SafeToDecimal();
        var deltaPercent = (inputExchange - exchange.Exchange) / inputExchange;
        AssertHelper.IsTrue(deltaPercent < _thirdPartOptions.CurrentValue.Alchemy.EffectivePricePercentage,
            "Invalid crypto price,from={} to={} input={}, latest={}", orderInput.Crypto, CommonConstant.USDT,
            inputExchange, exchange.Exchange);

        _objectMapper.Map(thirdPartOrderInput, orderDto);
        orderDto.Id = orderId;
        orderDto.Status = OrderStatusType.Created.ToString();
        orderDto.CryptoDecimals = token.Decimals;
        orderDto.FeeInfo = new List<FeeItem> { NetworkFeeInElf(CommonConstant.MainChainId) };

        await DoSaveOrder(orderDto);
    }

    public async Task DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null)
    {
        var orderStatusGrain = _clusterClient.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.TreasuryOrderStatusInfoPrefix,
                orderDto.RampOrderId.ToString()));
        await orderStatusGrain.AddOrderStatusInfo(new OrderStatusInfoGrainDto
        {
            OrderId = orderDto.Id,
            ThirdPartOrderNo = orderDto.ThirdPartOrderId,
            OrderStatusInfo = new OrderStatusInfo
            {
                Status = orderDto.Status,
                LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds(),
                Extension = externalData.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(externalData),
            }
        });
        
        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderDto.Id);
        await treasuryOrderGrain.SaveOrUpdateAsync(orderDto);
        await _distributedEventBus.PublishAsync(new TreasuryOrderEto(orderDto));
    }
    
    internal string GetRealIp(HttpContext httpContext)
    {
        if (httpContext?.Request.Headers.IsNullOrEmpty() ?? true) return null;
        var ipArr = httpContext?.Request.Headers["X-Forwarded-For"].ToString().Split(',');
        return ipArr.IsNullOrEmpty() ? null : ipArr[0].Trim();
    }
}