using AElf.ExceptionHandler;
using CAServer.Monitor.Interceptor;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftOrderSettlementHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> NftReleaseResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
    };

    private readonly ILogger<NftOrderSettlementHandler> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    
    public NftOrderSettlementHandler(ILogger<NftOrderSettlementHandler> logger,
        INftCheckoutService nftCheckoutService)
    {
        _logger = logger;
        _nftCheckoutService = nftCheckoutService;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && NftReleaseResultStatus.Contains(eventData.Status);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NftOrderSettlementHandler exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT release result
        if (!Match(eventData)) return;
        _logger.LogInformation("NftOrderSettlementHandler Notify nft release, Id={Id}, Status={Status}", eventData.Id, eventData.Status);
        
        await _nftCheckoutService.GetProcessor(eventData.MerchantName)
            .SaveOrderSettlementAsync(eventData.Id);
    }
}