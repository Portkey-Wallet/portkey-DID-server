using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractTreasuryProcessor : IThirdPartTreasuryProcessor
{
    private readonly ILogger<AbstractTreasuryProcessor> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;

    public AbstractTreasuryProcessor(ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IClusterClient clusterClient, IObjectMapper objectMapper, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IThirdPartOrderProvider thirdPartOrderProvider, ILogger<AbstractTreasuryProcessor> logger,
        ITreasuryOrderProvider treasuryOrderProvider)
    {
        _tokenAppService = tokenAppService;
        _chainOptions = chainOptions;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _thirdPartOptions = thirdPartOptions;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _logger = logger;
        _treasuryOrderProvider = treasuryOrderProvider;
    }

    internal abstract Task<string> AdaptPriceInputAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext;

    internal abstract Task<TreasuryBaseResult> AdaptPriceOutputAsync(TreasuryPriceDto treasuryPriceDto);

    internal abstract Task<TreasuryOrderRequest> AdaptOrderInputAsync<TOrderInput>(TOrderInput orderInput)
        where TOrderInput : TreasuryBaseContext;

    public abstract Task<Tuple<bool, string>> CallBackThirdPart(TreasuryOrderDto orderDto);


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
                [CommonConstant.MainChainId] = await NetworkFeeInElf(CommonConstant.MainChainId)
            }
        };

        // Adapt the returned results of different suppliers.
        return await AdaptPriceOutputAsync(cryptoPrice);
    }

    public async Task<FeeItem> NetworkFeeInElf(string chainId)
    {
        var chainExists =
            _chainOptions.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo);
        AssertHelper.IsTrue(chainExists, "ChainInfo of {} not found", CommonConstant.MainChainId);
        AssertHelper.NotNull(chainInfo, "ChainInfo of {} empty", CommonConstant.MainChainId);

        var exchange = await _tokenAppService.GetAvgLatestExchangeAsync(CommonConstant.ELF, CommonConstant.USDT);
        AssertHelper.NotNull(exchange, "Query exchange form {} to {} failed", CommonConstant.ELF, CommonConstant.USDT);
        return FeeItem.Crypto(CommonConstant.ELF, chainInfo!.TransactionFee.ToString(CultureInfo.InvariantCulture),
            exchange.Exchange);
    }

    /// <summary>
    ///     For thirdPart notify treasury order status
    /// </summary>
    /// <param name="thirdPartOrderInput"></param>
    /// <typeparam name="TOrderInput"></typeparam>
    /// <returns></returns>
    public async Task NotifyOrderAsync<TOrderInput>(TOrderInput thirdPartOrderInput) where TOrderInput : TreasuryBaseContext
    {
        try
        {
            var orderInput = await AdaptOrderInputAsync(thirdPartOrderInput);
            AssertHelper.NotEmpty(orderInput.Address, "Empty address");
            var address = Address.FromBase58(orderInput.Address);
            AssertHelper.NotNull(address, "Invalid address");

            // Query and verify ramp order
            var rampOrderPager = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, 1)
                {
                    ThirdPartName = orderInput.ThirdPartName,
                    ThirdPartOrderNoIn = new List<string> { orderInput.ThirdPartOrderId }
                });
            AssertHelper.NotNull(rampOrderPager, "Ramp order query result empty");

            var rampOrder = rampOrderPager.Items.FirstOrDefault();
            AssertHelper.NotNull(rampOrder, "Ramp order not exists");
            AssertHelper.IsTrue(rampOrder!.Crypto == orderInput.Crypto, "Crypto symbol not match");
            AssertHelper.IsTrue(rampOrder.CryptoAmount == orderInput.CryptoAmount, "Crypto amount not match");
            AssertHelper.IsTrue(rampOrder.Address == orderInput.Address, "Address not match");
            AssertHelper.IsTrue(rampOrder.TransDirect == TransferDirectionType.TokenBuy.ToString(),
                "Invalid ramp order transDirect");

            // Query order grain, verify exists
            var orderId = GuidHelper.UniqId(orderInput.ThirdPartName, orderInput.ThirdPartOrderId);
            var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderId);

            var orderResp = await treasuryOrderGrain.GetAsync();
            AssertHelper.NotNull(orderResp, "Get order grain failed");
            var orderDto = orderResp.Data;
            AssertHelper.NotNull(orderDto, "Get order grain data failed");
            AssertHelper.IsTrue(orderDto.ThirdPartOrderId.IsNullOrEmpty(), "Order {} of {} exists {}",
                orderInput.ThirdPartOrderId, orderInput.ThirdPartName, orderId);

            // Crypto token price 
            var token = await _tokenAppService.GetTokenInfoAsync(CommonConstant.MainChainId, orderInput.Crypto);
            AssertHelper.NotNull(token, "Token {} not found", orderInput.Crypto);

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
            orderDto.FeeInfo = new List<FeeItem> { await NetworkFeeInElf(CommonConstant.MainChainId) };

            await _treasuryOrderProvider.DoSaveOrder(orderDto);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("AbstractTreasuryProcessor handle notify order failed: {Msg}", e.Message);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AbstractTreasuryProcessor handle notify order error!");
            throw new UserFriendlyException("Internal error, please try again later");
        }
    }


    /// <summary>
    ///     Order Status of Callback Tripartite Service
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task CallBackAsync(Guid orderId)
    {
        TreasuryOrderDto orderDto = null;
        try
        {
            var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderId);

            var orderResp = await treasuryOrderGrain.GetAsync();
            AssertHelper.NotNull(orderResp, "Get order grain failed");
            orderDto = orderResp.Data;
            AssertHelper.NotNull(orderDto, "Get order grain data failed");

            var (success, callBackResult) = await CallBackThirdPart(orderDto);
            orderDto.CallbackCount++;
            orderDto.CallbackTime = DateTime.UtcNow.ToUtcMilliSeconds();
            orderDto.CallBackResult = callBackResult;
            orderDto.CallbackStatus =
                success ? TreasuryCallBackStatus.Success.ToString() : TreasuryCallBackStatus.Failed.ToString();
            
            await _treasuryOrderProvider.DoSaveOrder(orderDto, OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.CallBackStatus, orderDto.CallbackStatus)
                .Add(ExtensionKey.CallBackResult, callBackResult)
                .Build());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Treasury order callback error, orderId={OrderId}, status={Status}", orderId,
                orderDto?.Status ?? "null");
        }
    }
}