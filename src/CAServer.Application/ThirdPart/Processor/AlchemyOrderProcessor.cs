using System;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace CAServer.ThirdPart.Processor;

public class AlchemyOrderProcessor : AbstractThirdPartOrderProcessor
{
    private const string SignatureField = "signature";

    private readonly AlchemyOptions _alchemyOptions;
    private readonly ILogger<AlchemyOrderProcessor> _logger;

    public AlchemyOrderProcessor(ILogger<AlchemyOrderProcessor> logger, IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider, IOptions<ThirdPartOptions> thirdPartOptions)
        : base(logger, clusterClient, thirdPartOrderProvider)
    {
        _logger = logger;
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
        AssertHelper.IsTrue(achNftOrderRequest?.AppId == _alchemyOptions.AppId, "Invalid alchemy appId {AppId}",
            achNftOrderRequest?.AppId);
        
        // verify signature 
        var signSource = ThirdPartHelper.ConvertObjectToSortedString(achNftOrderRequest, SignatureField);
        var signature = AlchemyHelper.HmacSign(signSource, _alchemyOptions.AppSecret);
        _logger.LogInformation("Verify Alchemy signature, signature={Signature}, signSource={SignSource}",
            signSource, signature);
        AssertHelper.IsTrue(signature == achNftOrderRequest?.Signature, "Invalid alchemy signature {InputSign}",
            achNftOrderRequest?.Signature);
        
        return achNftOrderRequest;
    }

    public override void FillOrderData(IThirdPartNftOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftPartOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftPartOrderRequestDto;
        
        
        
        
        
    }

    public override void FillNftOrderData(IThirdPartNftOrderUpdateRequest input, NftOrderGrainDto orderGrainDto)
    {
        // verify input type and data 
        AssertHelper.IsTrue(input is AlchemyNftPartOrderRequestDto, "Invalid alchemy nft-order data");
        var achNftOrderRequest = input as AlchemyNftPartOrderRequestDto;
        
        
        
        
    }
}