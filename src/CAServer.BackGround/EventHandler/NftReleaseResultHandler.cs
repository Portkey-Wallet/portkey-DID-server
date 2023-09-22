using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftReleaseResultHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> NftReleaseResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
        OrderStatusType.TransferFailed.ToString(),
    };

    private readonly ILogger<NftReleaseResultHandler> _logger;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;

    public NftReleaseResultHandler(ILogger<NftReleaseResultHandler> logger,
        IThirdPartNftOrderProcessorFactory thirdPartNftOrderProcessorFactory)
    {
        _logger = logger;
        _thirdPartNftOrderProcessorFactory = thirdPartNftOrderProcessorFactory;
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
            await _thirdPartNftOrderProcessorFactory.GetProcessor(eventData.MerchantName)
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