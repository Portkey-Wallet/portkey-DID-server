using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Processors;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
using ChainOptions = CAServer.Options.ChainOptions;

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
    private readonly IContractProvider _contractProvider;
    private readonly ITokenProvider _tokenProvider;


    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithAElfTypesConverters()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();


    public AbstractTreasuryProcessor(ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IClusterClient clusterClient, IObjectMapper objectMapper, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IThirdPartOrderProvider thirdPartOrderProvider, ILogger<AbstractTreasuryProcessor> logger,
        ITreasuryOrderProvider treasuryOrderProvider, IContractProvider contractProvider, ITokenProvider tokenProvider)
    {
        _tokenAppService = tokenAppService;
        _chainOptions = chainOptions;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _thirdPartOptions = thirdPartOptions;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _logger = logger;
        _treasuryOrderProvider = treasuryOrderProvider;
        _contractProvider = contractProvider;
        _tokenProvider = tokenProvider;
    }

    internal abstract Task<string> AdaptPriceInputAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext;

    internal abstract Task<TreasuryBaseResult> AdaptPriceOutputAsync(TreasuryPriceDto treasuryPriceDto);

    internal abstract Task<TreasuryOrderRequest> AdaptOrderInputAsync<TOrderInput>(TOrderInput orderInput)
        where TOrderInput : TreasuryBaseContext;

    public abstract Task<Tuple<bool, string>> CallBackThirdPartAsync(TreasuryOrderDto orderDto);


    /// <summary>
    ///     ThirdPart provider name enums
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
    public async Task NotifyOrderAsync<TOrderInput>(TOrderInput thirdPartOrderInput)
        where TOrderInput : TreasuryBaseContext
    {
        try
        {
            var orderInput = await AdaptOrderInputAsync(thirdPartOrderInput);
            AssertHelper.NotEmpty(orderInput.Address, "Empty address");
            var address = Address.FromBase58(orderInput.Address);
            AssertHelper.NotNull(address, "Invalid address");

            var exchange = await _tokenAppService.GetAvgLatestExchangeAsync(orderInput.Crypto, CommonConstant.USDT);
            var inputExchange = orderInput.CryptoPrice.SafeToDecimal();
            var deltaPercent = (inputExchange - exchange.Exchange) / inputExchange;
            AssertHelper.IsTrue(deltaPercent < _thirdPartOptions.CurrentValue.Alchemy.EffectivePricePercentage,
                "Invalid crypto price,from={} to={} input={}, latest={}", orderInput.Crypto, CommonConstant.USDT,
                inputExchange, exchange.Exchange);

            var feeInfo = new List<FeeItem> { await NetworkFeeInElf(CommonConstant.MainChainId) };

            var pendingOrderGrainDto = new PendingTreasuryOrderDto
            {
                TreasuryOrderRequest = orderInput,
                TokenExchange = exchange,
                FeeInfo = feeInfo,
                ExpireTime = DateTime.UtcNow.ToUtcMilliSeconds() +
                             _thirdPartOptions.CurrentValue.Timer.PendingTreasuryOrderExpireSeconds * 1000,
                ThirdPartName = orderInput.ThirdPartName,
                ThirdPartOrderId = orderInput.ThirdPartOrderId,
                Status = OrderStatusType.Pending.ToString()
            };

            // It is likely that the ramp order has not received the webhook yet.
            _logger.LogInformation(
                "Pending treasury saved, ramp order not webhook yet, thirdPartName={Name}, thirdPartId={ThirdPartId}, id={Id}",
                pendingOrderGrainDto.ThirdPartName, pendingOrderGrainDto.ThirdPartOrderId, pendingOrderGrainDto.Id);
            await _treasuryOrderProvider.AddOrUpdatePendingTreasuryOrderAsync(pendingOrderGrainDto);
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
    ///     Processing previously pending Treasury orders,
    ///     by which time Ramp Order should have received the webhook
    /// </summary>
    /// <param name="rampOrder"></param>
    /// <param name="pendingTreasuryOrder"></param>
    public async Task HandlePendingTreasuryOrderAsync(OrderDto rampOrder, PendingTreasuryOrderDto pendingTreasuryOrder)
    {
        _logger.LogInformation(
            "Handle pending treasury order, rampOrderId={RampOrder}, thirdPartName={ThirdPartName}, thirdPartId={ThirdPartId}",
            rampOrder.Id, pendingTreasuryOrder.ThirdPartName, pendingTreasuryOrder.ThirdPartOrderId);

        var orderInput = pendingTreasuryOrder.TreasuryOrderRequest;
        AssertHelper.IsTrue(
            rampOrder!.Status.NotNullOrEmpty() && rampOrder.Status != OrderStatusType.Initialized.ToString(),
            "Ramp order still in state {}", rampOrder.Status);
        AssertHelper.IsTrue(rampOrder!.Crypto == orderInput.Crypto, "Crypto symbol not match");

        // rampAmount = treasuryAmount - networkFee
        // cryptoAmount in pendingTreasuryOrder = cryptoAmount + networkFeee
        var totalFeeInUsdt = pendingTreasuryOrder.FeeInfo
            .Select(fee => fee.Amount.SafeToDecimal() * fee.SymbolPriceInUsdt.SafeToDecimal()).Sum();
        var totalFeeInCrypto = totalFeeInUsdt * pendingTreasuryOrder.TokenExchange.Exchange;
        var cryptoAmountDiff = rampOrder.CryptoQuantity.SafeToDecimal() - totalFeeInCrypto -
                               orderInput.CryptoAmount.SafeToDecimal();
        var cryptoAmountDiffPercent = Math.Abs(cryptoAmountDiff) / rampOrder.CryptoQuantity.SafeToDecimal();
        _logger.LogInformation(
            "HandlePendingTreasuryOrderAsync, crypto={Crypto}, totalFee={Fee}, rampAmount={RampAmount}, treasuryAmount={TreasuryAmount}, diffPercent={Percent}",
            orderInput.Crypto, totalFeeInCrypto, rampOrder.CryptoQuantity,
            pendingTreasuryOrder.TreasuryOrderRequest.CryptoAmount, cryptoAmountDiff);
        AssertHelper.IsTrue(
            cryptoAmountDiffPercent <= _thirdPartOptions.CurrentValue.TreasuryOptions.ValidAmountPercent,
            "Crypto amount not match, rampOrderAmount={}, treasuryOrderAmount={}", rampOrder.CryptoQuantity,
            orderInput.CryptoAmount);
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
        AssertHelper.IsTrue(orderDto.ThirdPartOrderId.IsNullOrEmpty(), "Treasury order exists, {}-{}-{}",
            orderInput.ThirdPartOrderId, orderInput.ThirdPartName, orderId);

        // Crypto token price 
        var token = await _tokenProvider.GetTokenInfoAsync(CommonConstant.MainChainId, orderInput.Crypto);
        var decimals = token?.Decimals ?? (orderDto.Crypto == CommonConstant.ELF ? 8 :
            orderDto.Crypto == CommonConstant.USDT ? 6 : -1);
        AssertHelper.IsTrue(decimals >= 0, "Invalid decimals of symbol {}", orderInput.Crypto);

        _objectMapper.Map(orderInput, orderDto);
        orderDto.Id = orderId;
        orderDto.Status = OrderStatusType.Created.ToString();
        orderDto.CryptoDecimals = token!.Decimals;
        orderDto.FeeInfo = pendingTreasuryOrder.FeeInfo;
        orderDto.TokenExchanges = new List<TokenExchange> { pendingTreasuryOrder.TokenExchange };
        orderDto.RampOrderId = rampOrder.Id;
        orderDto.Fiat = rampOrder.Fiat;
        orderDto.FiatAmount = rampOrder.FiatAmount.SafeToDecimal();

        await _treasuryOrderProvider.DoSaveOrderAsync(orderDto);

        var pendingOrderGrain = _clusterClient.GetGrain<IPendingTreasuryOrderGrain>(
            IPendingTreasuryOrderGrain.GenerateId(pendingTreasuryOrder.ThirdPartName,
                pendingTreasuryOrder.ThirdPartOrderId));
        var pendingOrderGrainDto = await pendingOrderGrain.GetAsync();
        if (pendingOrderGrainDto != null)
        {
            _logger.LogInformation(
                "Pending treasury exists, will update to finish, thirdPartName={Name}, thirdPartId={ThirdPartId}, id={Id}",
                pendingOrderGrainDto.ThirdPartName, pendingOrderGrainDto.ThirdPartOrderId, pendingOrderGrainDto.Id);
            pendingOrderGrainDto.Status = OrderStatusType.Finish.ToString();
            await _treasuryOrderProvider.AddOrUpdatePendingTreasuryOrderAsync(pendingOrderGrainDto);
        }
    }

    public async Task<CommonResponseDto<Empty>> RefreshTransferMultiConfirmAsync(Guid orderId, long chainHeight,
        long confirmedHeight)
    {
        var validStatus = new List<string>
        {
            OrderStatusType.Transferring.ToString(),
            OrderStatusType.Transferred.ToString()
        };
        try
        {
            var orderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderId);
            var orderResult = await orderGrain.GetAsync();
            AssertHelper.IsTrue(orderResult.Success && orderResult.Data != null && orderResult.Data.Id == orderId,
                "Get treasury order failed");

            var order = orderResult.Data;
            AssertHelper.IsTrue(validStatus.Contains(order!.Status), "Invalid status {}", order.Status);
            AssertHelper.NotEmpty(order.TransactionId, "Transaction id not exists");

            var transactionResult =
                await _contractProvider.GetTransactionResultAsync(CommonConstant.MainChainId, order.TransactionId);
            _logger.LogDebug("RefreshTransferMultiConfirmAsync, orderId={OrderId}, tx={Tx}, txHeight={TxHeight}",
                orderId, transactionResult.TransactionId, transactionResult.BlockNumber);

            // update order status
            var newStatus = TransactionState.IsStateSuccessful(transactionResult.Status)
                ? transactionResult.BlockNumber <= confirmedHeight || chainHeight >=
                transactionResult.BlockNumber + _thirdPartOptions.CurrentValue.Timer.TransactionConfirmHeight
                    ? OrderStatusType.Finish.ToString()
                    : OrderStatusType.Transferred.ToString()
                : OrderStatusType.TransferFailed.ToString();
            AssertHelper.IsTrue(order.Status != newStatus,
                "Order status not changed, status={Status}, txBlock={Height}",
                transactionResult.Status, transactionResult.BlockNumber);

            // Record transfer data when filed
            var extraInfo = newStatus == OrderStatusType.TransferFailed.ToString()
                ? OrderStatusExtensionBuilder.Create()
                    .Add(ExtensionKey.TxResult, JsonConvert.SerializeObject(transactionResult, JsonSerializerSettings))
                    .Build()
                : OrderStatusExtensionBuilder.Create()
                    .Add(ExtensionKey.ChainHeight, chainHeight.ToString())
                    .Add(ExtensionKey.ChainLib, confirmedHeight.ToString())
                    .Build();

            order.Status = newStatus;
            await _treasuryOrderProvider.DoSaveOrderAsync(order, extraInfo);
            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(
                "RefreshTransferMultiConfirmAsync error, orderId={OrderId}, chainHeight={Height}, lib={Lib}", orderId,
                chainHeight, confirmedHeight);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "RefreshTransferMultiConfirmAsync error, orderId={OrderId}, chainHeight={Height}, lib={Lib}", orderId,
                chainHeight, confirmedHeight);
            return new CommonResponseDto<Empty>().Error(e);
        }
    }

    /// <summary>
    ///     Order Status of Callback Tripartite Service
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<Empty>> CallBackAsync(Guid orderId)
    {
        TreasuryOrderDto orderDto = null;
        try
        {
            var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderId);

            var orderResp = await treasuryOrderGrain.GetAsync();
            AssertHelper.NotNull(orderResp, "Get order grain failed");
            orderDto = orderResp.Data;
            AssertHelper.NotNull(orderDto, "Get order grain data failed");

            var (success, callBackResult) = await CallBackThirdPartAsync(orderDto);
            orderDto.CallbackCount++;
            orderDto.CallbackTime = DateTime.UtcNow.ToUtcMilliSeconds();
            orderDto.CallBackResult = callBackResult;
            orderDto.CallbackStatus =
                success ? TreasuryCallBackStatus.Success.ToString() : TreasuryCallBackStatus.Failed.ToString();

            await _treasuryOrderProvider.DoSaveOrderAsync(orderDto, OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.CallBackStatus, orderDto.CallbackStatus)
                .Add(ExtensionKey.CallBackResult, callBackResult)
                .Build());

            var resp = new CommonResponseDto<Empty>();
            if (!success) resp.Error(callBackResult);
            return resp;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Treasury order callback error, orderId={OrderId}, status={Status}", orderId,
                orderDto?.Status ?? "null");
            return new CommonResponseDto<Empty>().Error(e);
        }
    }
}