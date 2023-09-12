using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;
using Orleans;

namespace CAServer.ThirdPart.Processor;

public class AlchemyNftOrderProcessor : AbstractThirdPartNftOrderProcessor
{
    private readonly AlchemyProvider _alchemyProvider;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly ILogger<AlchemyNftOrderProcessor> _logger;

    public AlchemyNftOrderProcessor(ILogger<AlchemyNftOrderProcessor> logger, IClusterClient clusterClient,
        IOptions<ThirdPartOptions> thirdPartOptions,
        AlchemyProvider alchemyProvider, IOrderStatusProvider orderStatusProvider)
        : base(logger, clusterClient, thirdPartOptions, orderStatusProvider)
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
        AssertHelper.IsTrue(input is Dictionary<string, object>, "Invalid input");

        var ipnutDict = input as Dictionary<string, object>;
        var hasAppId = ipnutDict.TryGetValue(AlchemyHelper.AppIdField, out var appId);
        var hasSignature = ipnutDict.TryGetValue(AlchemyHelper.SignatureField, out var inputSignature);

        AssertHelper.IsTrue(hasAppId && hasSignature && appId is string && inputSignature is string,
            "Invalid alchemy order input {appId} - {sign}", appId, inputSignature);

        // verify signature 
        var signSource = ThirdPartHelper.ConvertObjectToSortedString(input,
            AlchemyHelper.SignatureField, AlchemyHelper.IdField);
        var signature = AlchemyHelper.HmacSign(signSource, _alchemyOptions.NftAppSecret);
        _logger.LogDebug("Verify Alchemy signature, signature={Signature}, signSource={SignSource}",
            signature, signSource);
        AssertHelper.IsTrue(signature == (string)inputSignature,
            "Invalid alchemy signature={InputSign}, signSource={SignSource}",
            inputSignature, signSource);

        var achNftOrderRequest = JsonConvert.DeserializeObject<AlchemyNftOrderDto>(
            JsonConvert.SerializeObject(input), HttpProvider.DefaultJsonSettings);
        // fill orderId 
        achNftOrderRequest.Id = Guid.Parse(achNftOrderRequest.MerchantOrderNo);
        // mapping status
        achNftOrderRequest.Status = AlchemyHelper.GetOrderStatus(achNftOrderRequest.Status);
        return achNftOrderRequest;
    }

    public override async Task<IThirdPartValidOrderUpdateRequest> QueryNftOrderAsync(Guid orderId)
    {
        return await _alchemyProvider.GetNftTrade(new AlchemyNftReleaseNoticeRequestDto()
        {
            OrderNo = orderId.ToString()
        });
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