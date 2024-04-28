using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DistributedLocking;

namespace CAServer.ThirdPart.Processor.NFT;

public class AlchemyNftOrderProcessor : AbstractThirdPartNftOrderProcessor
{
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly ILogger<AlchemyNftOrderProcessor> _logger;
    private readonly ISecretProvider _secretProvider;


    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .WithAElfTypesConverters()
        .Build();

    public AlchemyNftOrderProcessor(ILogger<AlchemyNftOrderProcessor> logger, IClusterClient clusterClient,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, AlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider, IContractProvider contractProvider,
        IAbpDistributedLock distributedLock, IThirdPartOrderAppService thirdPartOrderAppService,
        ITokenAppService tokenAppService, ISecretProvider secretProvider)
        : base(logger, clusterClient, thirdPartOptions, orderStatusProvider, contractProvider, distributedLock,
            thirdPartOrderAppService)
    {
        _logger = logger;
        _alchemyProvider = alchemyProvider;
        _thirdPartOptions = thirdPartOptions;
        _tokenAppService = tokenAppService;
        _secretProvider = secretProvider;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    public AlchemyOptions AlchemyOptions()
    {
        return _thirdPartOptions.CurrentValue.Alchemy;
    }

    public override async Task<IThirdPartValidOrderUpdateRequest> VerifyNftOrderAsync(
        IThirdPartNftOrderUpdateRequest input)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftOrderRequestDto, "Invalid input");

        var inputDict = input as AlchemyNftOrderRequestDto;
        _ = inputDict.TryGetValue(AlchemyHelper.AppIdField, out var appId);
        _ = inputDict.TryGetValue(AlchemyHelper.SignatureField, out var inputSignature);

        AssertHelper.NotEmpty(appId, "Empty appId");
        AssertHelper.NotEmpty(inputSignature, "Empty signature");

        // verify signature 
        var signSource = ThirdPartHelper.ConvertObjectToSortedString(input,
            AlchemyHelper.SignatureField, AlchemyHelper.IdField);
        var signature = await _secretProvider.GetAlchemyHmacSignAsync(AlchemyOptions().NftAppId, signSource);
        _logger.LogDebug("Verify Alchemy signature, signature={Signature}, signSource={SignSource}",
            signature, signSource);
        AssertHelper.IsTrue(signature == inputSignature,
            "Invalid alchemy signature={InputSign}, signSource={SignSource}",
            inputSignature, signSource);

        var achNftOrderRequest = JsonConvert.DeserializeObject<AlchemyNftOrderDto>(
            JsonConvert.SerializeObject(input), JsonSerializerSettings);
        // fill orderId 
        achNftOrderRequest.Id = Guid.Parse(achNftOrderRequest.MerchantOrderNo);
        // mapping status
        achNftOrderRequest.Status = AlchemyHelper.GetOrderStatus(achNftOrderRequest.Status).ToString();
        return achNftOrderRequest;
    }

    public override async Task<IThirdPartValidOrderUpdateRequest> QueryNftOrderAsync(Guid orderId)
    {
        var alchemyOrder = await _alchemyProvider.GetNftTradeAsync(new AlchemyNftReleaseNoticeRequestDto()
        {
            OrderNo = orderId.ToString()
        });
        alchemyOrder.Id = Guid.Parse(alchemyOrder.MerchantOrderNo);
        alchemyOrder.Status = AlchemyHelper.GetOrderStatus(alchemyOrder.Status).ToString();
        return alchemyOrder;
    }

    public override bool FillOrderData(IThirdPartValidOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftOrderDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftOrderDto;

        orderGrainDto.ThirdPartOrderNo = orderGrainDto.ThirdPartOrderNo.DefaultIfEmpty(achNftOrderRequest?.OrderNo);
        orderGrainDto.Fiat = orderGrainDto.Fiat.DefaultIfEmpty(achNftOrderRequest?.Fiat);
        orderGrainDto.FiatAmount = orderGrainDto.FiatAmount.DefaultIfEmpty(achNftOrderRequest?.Amount);
        orderGrainDto.Status = achNftOrderRequest?.Status;
        orderGrainDto.PaymentMethod = orderGrainDto.PaymentMethod.DefaultIfEmpty(achNftOrderRequest?.PayType);
        orderGrainDto.TxTime = orderGrainDto.TxTime.DefaultIfEmpty(achNftOrderRequest?.PayTime);
        orderGrainDto.MerchantName = orderGrainDto.MerchantName.DefaultIfEmpty(ThirdPartName());
        return true;
    }

    public override async Task<CommonResponseDto<Empty>> DoNotifyNftReleaseAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto)
    {
        try
        {
            await _alchemyProvider.NoticeNftReleaseResultAsync(new AlchemyNftReleaseNoticeRequestDto
            {
                MerchantOrderNo = orderGrainDto.Id.ToString(),
                OrderNo = orderGrainDto.ThirdPartOrderNo,
                ReleaseStatus = orderGrainDto.Status == OrderStatusType.Finish.ToString()
                    ? NftOrderWebhookStatus.SUCCESS.ToString()
                    : NftOrderWebhookStatus.FAIL.ToString(),
                TransactionHash = orderGrainDto.TransactionId,
                ReleaseTime = DateTime.UtcNow.ToUtcMilliSeconds().ToString(),
                PictureNumber = nftOrderGrainDto.NftSymbol,
            });
            return new CommonResponseDto<Empty>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Notify NFT release result to {ThirdPartName} fail", orderGrainDto.MerchantName);
            return new CommonResponseDto<Empty>().Error(e);
        }
    }

    public override async Task<OrderSettlementGrainDto> FillOrderSettlementAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto,
        OrderSettlementGrainDto orderSettlementGrainDto, long? finishTime = null)
    {
        // When the finishTime is empty, query the latest price,
        // otherwise query the historical price.
        var finishTimeLong = finishTime ?? DateTime.UtcNow.ToUtcMilliSeconds();
        var finishDateTime = finishTime == null ? (DateTime?)null : TimeHelper.GetDateTimeFromTimeStamp(finishTimeLong);

        var binanceExchange = finishTime == null
            ? await _tokenAppService.GetLatestExchangeAsync(ExchangeProviderName.Binance.ToString(),
                orderGrainDto.Crypto,
                CommonConstant.USDT)
            : await _tokenAppService.GetHistoryExchangeAsync(ExchangeProviderName.Binance.ToString(),
                orderGrainDto.Crypto,
                CommonConstant.USDT, (DateTime)finishDateTime);

        var okxExchange = finishTime == null
            ? await _tokenAppService.GetLatestExchangeAsync(ExchangeProviderName.Okx.ToString(), orderGrainDto.Crypto,
                CommonConstant.USDT)
            : await _tokenAppService.GetHistoryExchangeAsync(ExchangeProviderName.Okx.ToString(), orderGrainDto.Crypto,
                CommonConstant.USDT, (DateTime)finishDateTime);

        var cryptoPrice = orderGrainDto.CryptoAmount.SafeToDecimal();

        if (orderSettlementGrainDto.BinanceSettlementAmount == null && binanceExchange != null)
        {
            orderSettlementGrainDto.BinanceExchange = binanceExchange.Exchange;
            orderSettlementGrainDto.BinanceSettlementAmount = cryptoPrice * binanceExchange.Exchange;
        }

        if (orderSettlementGrainDto.OkxSettlementAmount == null && okxExchange != null)
        {
            orderSettlementGrainDto.OkxExchange = okxExchange.Exchange;
            orderSettlementGrainDto.OkxSettlementAmount = cryptoPrice * okxExchange.Exchange;
        }

        orderSettlementGrainDto.SettlementCurrency = CommonConstant.USDT;
        return orderSettlementGrainDto;
    }
}