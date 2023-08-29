using System;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAServer.ThirdPart.Processor;

public class AlchemyOrderProcessor : AbstractThirdPartOrderProcessor
{
    private readonly ILogger<AlchemyOrderProcessor> _logger;
    
    public AlchemyOrderProcessor(ILogger<AlchemyOrderProcessor> logger, IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider) : base(logger,
        clusterClient, thirdPartOrderProvider)
    {
        _logger = logger;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    public override IThirdPartNftOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        AssertHelper.IsTrue(input is AlchemyNftPartOrderRequestDto achNftOrderRequest, "Invalid alchemy nft-order data");
        
        throw new NotImplementedException();
    }

    public override void FillOrderData(IThirdPartNftOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        throw new NotImplementedException();
    }

    public override void FillNftOrderData(IThirdPartNftOrderUpdateRequest input, NftOrderGrainDto orderGrainDto)
    {
        throw new NotImplementedException();
    }
}