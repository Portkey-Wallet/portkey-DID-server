using AElf.ExceptionHandler;
using CAServer.Common;
using CAServer.Monitor.Interceptor;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
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
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderTransferHandler(ILogger<NftOrderTransferHandler> logger,
        INftCheckoutService nftCheckoutService, IOrderStatusProvider orderStatusProvider)
    {
        _logger = logger;
        _nftCheckoutService = nftCheckoutService;
        _orderStatusProvider = orderStatusProvider;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && ResultStatus.Contains(eventData.Status);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NftOrderSettlementHandler exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT pay result
        if (!Match(eventData)) return;

        var orderId = eventData.Id;
        var status = eventData.Status;
        var thirdPart = eventData.MerchantName;
        _logger.LogInformation("NftOrderSettlementHandler HandleAsync nft order pay result fail, Id={Id}, Status={Status}", orderId, status);
        
        await _nftCheckoutService.GetProcessor(thirdPart).SettlementTransferAsync(orderId);
    }
}