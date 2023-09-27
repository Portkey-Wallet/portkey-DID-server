using CAServer.BackGround.Options;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Hangfire;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider;

public interface INftOrderThirdPartOrderStatusWorker
{
    Task Handle();
}

public class NftOrderThirdPartOrderStatusWorker : INftOrderThirdPartOrderStatusWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderThirdPartOrderStatusWorker> _logger;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;

    public NftOrderThirdPartOrderStatusWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        IThirdPartNftOrderProcessorFactory thirdPartNftOrderProcessorFactory, ILogger<NftOrderThirdPartOrderStatusWorker> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptions<ThirdPartOptions> thirdPartOptions, IOptions<TransactionOptions> transactionOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartNftOrderProcessorFactory = thirdPartNftOrderProcessorFactory;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _transactionOptions = transactionOptions.Value;
        _thirdPartOptions = thirdPartOptions.Value;
    }

    /// <summary>
    ///     Compensate unprocessed order data from ThirdPart webhook.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task Handle()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix + "HandleUnCompletedNftOrderPayResultRefresh");
        if (handle == null)
        {
            _logger.LogWarning("HandleUnCompletedNftOrderPayResultRefresh running, skip");
            return;
        }

        _logger.LogDebug("HandleUnCompletedThirdPartResultNotify start");
        const int pageSize = 100;
        var minutesAgo = _thirdPartOptions.Timer.HandleUnCompletedOrderMinuteAgo;
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
                        OrderStatusType.Initialized.ToString(),
                        OrderStatusType.Created.ToString(),
                    },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Data.IsNullOrEmpty()) break;

            lastModifyTimeLt = pendingData.Data.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Data)
            {
                callbackResults.Add(_thirdPartNftOrderProcessorFactory
                    .GetProcessor(orderDto.MerchantName)
                    .RefreshThirdPartNftOrderAsync(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            total += handleCount;
        }

        _logger.LogDebug("HandleUnCompletedThirdPartResultNotify finish, total:{Total}", total);
    }
}