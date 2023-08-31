using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace CAServer.ThirdPart.Processor;

public class AlchemyNftOrderProcessor : AbstractThirdPartNftOrderProcessor
{
    private const string SignatureField = "signature";

    private readonly AlchemyProvider _alchemyProvider;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly ILogger<AlchemyNftOrderProcessor> _logger;

    public AlchemyNftOrderProcessor(ILogger<AlchemyNftOrderProcessor> logger, IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider, IOptions<ThirdPartOptions> thirdPartOptions,
        AlchemyProvider alchemyProvider)
        : base(logger, clusterClient, thirdPartOrderProvider, thirdPartOptions)
    {
        _logger = logger;
        _alchemyProvider = alchemyProvider;
        _alchemyOptions = thirdPartOptions.Value.Alchemy;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    public override IThirdPartValidOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftOrderRequestDto;
        AssertHelper.NotNull(achNftOrderRequest, "Empty input");
        AssertHelper.IsTrue(achNftOrderRequest.AppId == _alchemyOptions.AppId, "Invalid alchemy appId {AppId}",
            achNftOrderRequest?.AppId);

        // verify signature 
        var signSource = ThirdPartHelper.ConvertObjectToSortedString(achNftOrderRequest, SignatureField);
        var signature = AlchemyHelper.HmacSign(signSource, _alchemyOptions.AppSecret);
        _logger.LogInformation("Verify Alchemy signature, signature={Signature}, signSource={SignSource}",
            signSource, signature);
        AssertHelper.IsTrue(signature == achNftOrderRequest?.Signature, "Invalid alchemy signature {InputSign}",
            achNftOrderRequest?.Signature);
        achNftOrderRequest.Id = Guid.Parse(achNftOrderRequest.MerchantOrderNo);

        return achNftOrderRequest;
    }

    public override async Task<IThirdPartValidOrderUpdateRequest> QueryNftOrderAsync(Guid orderId)
    {
        return await _alchemyProvider.QueryNftTrade(new AlchemyNftReleaseNoticeRequestDto()
        {
            OrderNo = orderId.ToString()
        });
    }

    public override bool FillOrderData(IThirdPartValidOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftOrderRequestDto;

        orderGrainDto.Fiat = orderGrainDto.Fiat.DefaultIfEmpty(achNftOrderRequest?.Fiat);
        orderGrainDto.FiatAmount = orderGrainDto.FiatAmount.DefaultIfEmpty(achNftOrderRequest?.Amount);
        orderGrainDto.Status = AlchemyHelper.GetOrderStatus(achNftOrderRequest?.Status);
        orderGrainDto.PaymentMethod = orderGrainDto.PaymentMethod.DefaultIfEmpty(achNftOrderRequest?.PayType);
        orderGrainDto.TxTime = orderGrainDto.TxTime.DefaultIfEmpty(achNftOrderRequest?.PayTime);
        orderGrainDto.MerchantName = orderGrainDto.MerchantName.DefaultIfEmpty(ThirdPartName());
        return true;
    }

    public override async Task<CommonResponseDto<Empty>> DoNotifyNftReleaseAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto)
    {
        var resp = await _alchemyProvider.NoticeNftReleaseResult(new AlchemyNftReleaseNoticeRequestDto
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
        return resp.Success.ToLower() == "success"
            ? new CommonResponseDto<Empty>()
            : new CommonResponseDto<Empty>().Error(resp.ReturnCode, resp.ReturnMsg);
    }
}