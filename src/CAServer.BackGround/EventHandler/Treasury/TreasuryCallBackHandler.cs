using CAServer.Common;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler.Treasury;

public class TreasuryCallBackHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly ILogger<TreasuryCallBackHandler> _logger;
    private readonly ITreasuryProcessorFactory _treasuryProcessorFactory;

    public TreasuryCallBackHandler(ITreasuryProcessorFactory treasuryProcessorFactory,
        ILogger<TreasuryCallBackHandler> logger)
    {
        _treasuryProcessorFactory = treasuryProcessorFactory;
        _logger = logger;
    }

    private bool Match(TreasuryOrderEto eventData)
    {
        return eventData?.Data != null &&
               eventData.Data.TransferDirection == TransferDirectionType.TokenBuy.ToString() &&
               eventData.Data.Status == OrderStatusType.Finish.ToString() && eventData.Data.CallbackCount == 0;
    }

    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        if (!Match(eventData)) return;

        var orderDto = eventData.Data;
        try
        {
            var resp = await _treasuryProcessorFactory.Processor(orderDto.ThirdPartName).CallBackAsync(orderDto.Id);
            AssertHelper.IsTrue(resp.Success, resp.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TreasuryCallBackHandler error, orderId={OrderId}, status={Status}", orderDto.Id,
                orderDto.Status);
        }
    }
}