using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
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
    private readonly ILogger<AlchemyNftOrderProcessor> _logger;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .WithAElfTypesConverters()
        .Build();

    public AlchemyNftOrderProcessor(ILogger<AlchemyNftOrderProcessor> logger, IClusterClient clusterClient,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, AlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider, IContractProvider contractProvider,
        IAbpDistributedLock distributedLock)
        : base(logger, clusterClient, thirdPartOptions, orderStatusProvider, contractProvider, distributedLock)
    {
        _logger = logger;
        _alchemyProvider = alchemyProvider;
        _thirdPartOptions = thirdPartOptions;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    public AlchemyOptions AlchemyOptions()
    {
        return _thirdPartOptions.CurrentValue.Alchemy;
    }
    
    public override IThirdPartValidOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftOrderRequestDto, "Invalid input");

        var inputDict = input as AlchemyNftOrderRequestDto;
        var hasAppId = inputDict.TryGetValue(AlchemyHelper.AppIdField, out var appId);
        var hasSignature = inputDict.TryGetValue(AlchemyHelper.SignatureField, out var inputSignature);

        AssertHelper.NotEmpty(appId, "Empty appId");
        AssertHelper.NotEmpty(inputSignature, "Empty signature");

        // verify signature 
        var signSource = ThirdPartHelper.ConvertObjectToSortedString(input,
            AlchemyHelper.SignatureField, AlchemyHelper.IdField);
        var signature = AlchemyHelper.HmacSign(signSource, AlchemyOptions().NftAppSecret);
        _logger.LogDebug("Verify Alchemy signature, signature={Signature}, signSource={SignSource}",
            signature, signSource);
        AssertHelper.IsTrue(signature == (string)inputSignature,
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
        var alchemyOrder = await _alchemyProvider.GetNftTrade(new AlchemyNftReleaseNoticeRequestDto()
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
            await _alchemyProvider.NoticeNftReleaseResult(new AlchemyNftReleaseNoticeRequestDto
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
}