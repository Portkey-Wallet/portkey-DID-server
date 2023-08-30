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

public class AlchemyOrderProcessor : AbstractThirdPartOrderProcessor
{
    private const string SignatureField = "signature";

    private readonly AlchemyProvider _alchemyProvider;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly ILogger<AlchemyOrderProcessor> _logger;

    public AlchemyOrderProcessor(ILogger<AlchemyOrderProcessor> logger, IClusterClient clusterClient,
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

    public override IThirdPartNftOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftPartOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftPartOrderRequestDto;
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

    public override bool FillOrderData(IThirdPartNftOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftPartOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftPartOrderRequestDto;

        orderGrainDto.Fiat = orderGrainDto.Fiat.DefaultIfEmpty(achNftOrderRequest?.Fiat);
        orderGrainDto.FiatAmount = orderGrainDto.FiatAmount.DefaultIfEmpty(achNftOrderRequest?.Amount);
        orderGrainDto.Status = AlchemyHelper.GetOrderStatus(achNftOrderRequest?.Status);
        orderGrainDto.PaymentMethod = orderGrainDto.PaymentMethod.DefaultIfEmpty(achNftOrderRequest?.PayType);
        orderGrainDto.TxTime = orderGrainDto.TxTime.DefaultIfEmpty(achNftOrderRequest?.PayTime);
        orderGrainDto.MerchantName = orderGrainDto.MerchantName.DefaultIfEmpty(ThirdPartName());
        return true;
    }

    public override bool FillNftOrderData(IThirdPartNftOrderUpdateRequest input, NftOrderGrainDto orderGrainDto)
    {
        // do nothing
        return false;
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