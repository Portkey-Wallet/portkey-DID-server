using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftReleaseResultHandler : IDistributedEventHandler<OrderEto>
{
    private static readonly List<string> NftReleaseResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
        OrderStatusType.TransferFailed.ToString(),
    };

    private readonly ILogger<NftReleaseResultHandler> _logger;
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;

    public NftReleaseResultHandler(ILogger<NftReleaseResultHandler> logger,
        IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory)
    {
        _logger = logger;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && NftReleaseResultStatus.Contains(eventData.Status);
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT release result
        if (!Match(eventData)) return;

        try
        {
            await _thirdPartOrderProcessorFactory.GetProcessor(eventData.MerchantName)
                .NotifyNftReleaseAsync(eventData.Id);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Notify nft release result fail, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Notify nft release result error, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
            throw;
        }
    }
}