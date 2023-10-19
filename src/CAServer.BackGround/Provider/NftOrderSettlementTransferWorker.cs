using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Hangfire;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider;

public interface INftOrderSettlementTransferWorker
{
    Task Handle();
}

public class NftOrderSettlementTransferWorker : INftOrderSettlementTransferWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderSettlementTransferWorker> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;
    private readonly IContractProvider _contractProvider;


    public NftOrderSettlementTransferWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, ILogger<NftOrderSettlementTransferWorker> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptions<ThirdPartOptions> thirdPartOptions, IOptions<TransactionOptions> transactionOptions, IContractProvider contractProvider)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _nftCheckoutService = nftCheckoutService;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _contractProvider = contractProvider;
        _transactionOptions = transactionOptions.Value;
        _thirdPartOptions = thirdPartOptions.Value;
    }
    
    /// <summary>
    ///     fix uncompleted ELF transfer to merchant
    /// </summary>
    /// 
    [AutomaticRetry(Attempts = 0)]
    public async Task Handle()
    {
        _logger.LogDebug("NftOrderSettlementTransferWorker start");
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix + "NftOrderSettlementTransferWorker");
        if (handle == null)
        {
            _logger.LogWarning("NftOrderSettlementTransferWorker running, skip");
            return;
        }
        
        var chainStatus = await _contractProvider.GetChainStatus(CommonConstant.MainChainId);
        _logger.LogDebug("NftOrderSettlementTransferWorker chainHeight={Height} LIB: {LibHeight}", 
            chainStatus.BestChainHeight, chainStatus.LastIrreversibleBlockHeight);
        
        const int pageSize = 100;
        var secondsAgo = _thirdPartOptions.Timer.HandleUnCompletedSettlementTransferSecondsAgo;
        var lastModifyTimeLt = DateTime.UtcNow.AddSeconds(-secondsAgo).ToUtcMilliSeconds().ToString();
        var modifyTimeGt = DateTime.UtcNow.AddHours(-1).ToUtcMilliSeconds().ToString();
        var total = 0;
        var count = 0;
        while (true)
        {
            if (string.Compare(lastModifyTimeLt, modifyTimeGt, StringComparison.Ordinal) <= 0) break;
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeLt = lastModifyTimeLt,
                    StatusIn = new List<string>
                    {
                        OrderStatusType.StartTransfer.ToString(),
                        OrderStatusType.Transferring.ToString(),
                        OrderStatusType.Transferred.ToString(),
                    },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Data.IsNullOrEmpty()) break;
            total += pendingData.Data.Count;
            
            lastModifyTimeLt = pendingData.Data.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Data)
            {
                callbackResults.Add(_nftCheckoutService
                    .GetProcessor(orderDto.MerchantName)
                    .RefreshSettlementTransfer(orderDto.Id, chainStatus.BestChainHeight, chainStatus.LastIrreversibleBlockHeight));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            count += handleCount;
        }

        if (total > 0)
        {
            _logger.LogInformation("NftOrderSettlementTransferWorker finish, total:{Count}/{Total}", count, total);
        }
        else
        {
            _logger.LogDebug("NftOrderSettlementTransferWorker finish, total:{Count}/{Total}", count, total);
        }

    }
}