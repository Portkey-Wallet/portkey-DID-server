using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Hangfire;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider;


public class NftOrderSettlementTransferWorker : IJobWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderSettlementTransferWorker> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;
    private readonly IContractProvider _contractProvider;


    public NftOrderSettlementTransferWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, ILogger<NftOrderSettlementTransferWorker> logger,
        IAbpDistributedLock distributedLock,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, IOptions<TransactionOptions> transactionOptions,
        IContractProvider contractProvider)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _nftCheckoutService = nftCheckoutService;
        _logger = logger;
        _distributedLock = distributedLock;
        _contractProvider = contractProvider;
        _transactionOptions = transactionOptions.Value;
        _thirdPartOptions = thirdPartOptions;
    }

    /// <summary>
    ///     fix uncompleted ELF transfer to merchant
    /// </summary>
    /// 
    [AutomaticRetry(Attempts = 0)]
    public async Task HandleAsync()
    {
        _logger.LogDebug("NftOrderSettlementTransferWorker start");
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix +
                                                         "NftOrderSettlementTransferWorker");
        if (handle == null)
        {
            _logger.LogWarning("NftOrderSettlementTransferWorker running, skip");
            return;
        }

        var chainStatus = await _contractProvider.GetChainStatusAsync(CommonConstant.MainChainId);
        _logger.LogDebug("NftOrderSettlementTransferWorker chainHeight={Height} LIB: {LibHeight}",
            chainStatus.BestChainHeight, chainStatus.LastIrreversibleBlockHeight);

        var pageSize = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferPageSize;
        var secondsAgo = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferSecondsAgo;
        var lastModifyTimeLt = DateTime.UtcNow.AddSeconds(-secondsAgo).ToUtcMilliSeconds().ToString();
        var modifyTimeGt = DateTime.UtcNow
            .AddMinutes(-_thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferMinuteAgo).ToUtcMilliSeconds();
        var total = 0;
        var count = 0;
        while (true)
        {
            if (string.Compare(lastModifyTimeLt, modifyTimeGt.ToString(), StringComparison.Ordinal) <= 0) break;
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeGt = modifyTimeGt.ToString(),
                    LastModifyTimeLt = lastModifyTimeLt,
                    StatusIn = new List<string>
                    {
                        OrderStatusType.StartTransfer.ToString(),
                        OrderStatusType.Transferring.ToString(),
                        OrderStatusType.Transferred.ToString(),
                    },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Items.IsNullOrEmpty()) break;
            total += pendingData.Items.Count;

            lastModifyTimeLt = pendingData.Items.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Items)
            {
                var createTime = orderDto.NftOrderSection?.CreateTime;
                if (createTime == null || createTime < modifyTimeGt)
                {
                    _logger.LogInformation(
                        "NftOrderSettlementTransferWorker order too early, order={OrderId}, status={Status}",
                        orderDto.Id, orderDto.Status);
                    continue;
                }

                callbackResults.Add(_nftCheckoutService
                    .GetProcessor(orderDto.MerchantName)
                    .RefreshSettlementTransferAsync(orderDto.Id, chainStatus.BestChainHeight,
                        chainStatus.LastIrreversibleBlockHeight));
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