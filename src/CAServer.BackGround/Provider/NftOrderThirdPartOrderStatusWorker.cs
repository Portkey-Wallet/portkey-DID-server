using CAServer.BackGround.Options;
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

public class NftOrderThirdPartOrderStatusWorker : IJobWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderThirdPartOrderStatusWorker> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<TransactionOptions> _transactionOptions;

    public NftOrderThirdPartOrderStatusWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, ILogger<NftOrderThirdPartOrderStatusWorker> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, IOptionsMonitor<TransactionOptions> transactionOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _nftCheckoutService = nftCheckoutService;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _transactionOptions = transactionOptions;
        _thirdPartOptions = thirdPartOptions;
    }

    /// <summary>
    ///     Compensate unprocessed order data from ThirdPart webhook.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task HandleAsync()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.CurrentValue.LockKeyPrefix + "NftOrderThirdPartOrderStatusWorker");
        if (handle == null)
        {
            _logger.LogWarning("NftOrderThirdPartOrderStatusWorker running, skip");
            return;
        }

        _logger.LogDebug("NftOrderThirdPartOrderStatusWorker start");
        var pageSize = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedOrderPageSize;
        var minutesAgo = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedOrderMinuteAgo;
        var lastModifyTimeLt = DateTime.UtcNow.AddMinutes(-minutesAgo).ToUtcMilliSeconds().ToString();
        var modifyTimeGt = DateTime.UtcNow.AddHours(-1).ToUtcMilliSeconds().ToString();
        var total = 0;
        while (true)
        {
            if (string.Compare(lastModifyTimeLt, modifyTimeGt, StringComparison.Ordinal) <= 0) break;
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeLt = lastModifyTimeLt,
                    LastModifyTimeGt = modifyTimeGt,
                    StatusIn = new List<string>
                    {
                        // OrderStatusType.Initialized.ToString(),
                        OrderStatusType.Created.ToString(),
                    },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Items.IsNullOrEmpty()) break;

            lastModifyTimeLt = pendingData.Items.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Items)
            {
                callbackResults.Add(_nftCheckoutService
                    .GetProcessor(orderDto.MerchantName)
                    .RefreshThirdPartNftOrderAsync(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            total += handleCount;
        }
        
        if (total > 0)
        {
            _logger.LogInformation("NftOrderThirdPartOrderStatusWorker finish, total:{Total}", total);
        }
    }
}