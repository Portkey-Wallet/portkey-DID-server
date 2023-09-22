using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftOrderTransferHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> ResultStatus = new()
    {
        OrderStatusType.StartTransfer.ToString()
    };

    private readonly ILogger<NftOrderTransferHandler> _logger;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;

    public NftOrderTransferHandler(ILogger<NftOrderTransferHandler> logger,
        IThirdPartNftOrderProcessorFactory thirdPartNftOrderProcessorFactory)
    {
        _logger = logger;
        _thirdPartNftOrderProcessorFactory = thirdPartNftOrderProcessorFactory;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && ResultStatus.Contains(eventData.Status);
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT pay result
        if (!Match(eventData)) return;

        var orderId = eventData.Id;
        var status = eventData.Status;
        var thirdPart = eventData.MerchantName;
        try
        {
            await _thirdPartNftOrderProcessorFactory.GetProcessor(thirdPart).SettlementTransfer(orderId);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Handle nft order pay result fail, Id={Id}, Status={Status}", orderId, status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle nft order pay result error, Id={Id}, Status={Status}", orderId, status);
            throw;
        }
    }
}