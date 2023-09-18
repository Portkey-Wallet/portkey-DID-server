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

public interface INftOrderProvider
{
    Task HandleUnCompletedMerchantCallback();
    Task HandleUnCompletedThirdPartResultNotify();
    Task HandleUnCompletedNftOrderPayResultRefresh();
}

public class NftOrderProvider : INftOrderProvider, ISingletonDependency
{
    private const string LockKeyPrefix = "CAServer.BGD:NFT_Order_worker:";

    private readonly ILogger<NftOrderProvider> _logger;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;

    public NftOrderProvider(IThirdPartOrderProvider thirdPartOrderProvider,
        IThirdPartNftOrderProcessorFactory thirdPartNftOrderProcessorFactory, ILogger<NftOrderProvider> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptions<ThirdPartOptions> thirdPartOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartNftOrderProcessorFactory = thirdPartNftOrderProcessorFactory;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _thirdPartOptions = thirdPartOptions.Value;
    }


    /// <summary>
    ///     Compensate for NFT-order-pay-result not properly notified to Merchant.
    /// </summary>
    ///
    [AutomaticRetry(Attempts = 0)]
    public async Task HandleUnCompletedMerchantCallback()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + "HandleUnCompletedMerchantCallback");
        if (handle == null)
        {
            _logger.LogError("HandleUnCompletedMerchantCallback running, skip");
            return;
        }

        _logger.LogInformation("HandleUnCompletedMerchantCallback start");
        const int pageSize = 100;
        const int minCallbackCount = 1;
        var maxCallbackCount = _thirdPartOptions.Timer.NftCheckoutMerchantCallbackCount;

        // when WebhookCount > 0, WebhookTimeLt mast be exists
        var minusAgo = _thirdPartOptions.Timer.NftUnCompletedMerchantCallbackMinuteAgo;
        var lastWebhookTimeLt = DateTime.UtcNow.AddMinutes(- minusAgo).ToUtcString();
        var total = 0;
        while (true)
        {
            var pendingData = await _thirdPartOrderProvider.QueryNftOrderPagerAsync(
                new NftOrderQueryConditionDto(0, pageSize)
                {
                    WebhookCountGtEq = minCallbackCount,
                    WebhookCountLtEq = maxCallbackCount - 1,
                    WebhookStatus = NftOrderWebhookStatus.FAIL.ToString(),
                    WebhookTimeLt = lastWebhookTimeLt
                });
            if (pendingData.Data.IsNullOrEmpty()) break;

            lastWebhookTimeLt = pendingData.Data.Min(order => order.WebhookTime);

            var callbackResults = new List<Task<int>>();
            foreach (var orderDto in pendingData.Data)
            {
                callbackResults.Add(_orderStatusProvider.CallBackNftOrderPayResultAsync(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max callback-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Sum();
            total += handleCount;
            if (handleCount == 0) break;
        }

        _logger.LogInformation("HandleUnCompletedMerchantCallback finish, total:{Total}", total);
    }


    /// <summary>
    ///     Compensate for NFT-release-result not properly notified to ThirdPart.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task HandleUnCompletedThirdPartResultNotify()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + "HandleUnCompletedThirdPartResultNotify");
        if (handle == null)
        {
            _logger.LogError("HandleUnCompletedThirdPartResultNotify running, skip");
            return;
        }

        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify start");
        const int pageSize = 100;
        const int minNotifyCount = 1;
        var maxNotifyCount = _thirdPartOptions.Timer.NftCheckoutResultThirdPartNotifyCount;

        // when ThirdPartNotifyCount > 0, WebhookTimeLt mast be exists
        var minusAgo = _thirdPartOptions.Timer.NftUnCompletedThirdPartCallbackMinuteAgo;
        var lastWebhookTimeLt = DateTime.UtcNow.AddMinutes(-minusAgo).ToUtcString();
        var total = 0;
        while (true)
        {
            var pendingData = await _thirdPartOrderProvider.QueryNftOrderPagerAsync(
                new NftOrderQueryConditionDto(0, pageSize)
                {
                    ThirdPartNotifyCountGtEq = minNotifyCount,
                    ThirdPartNotifyCountLtEq = maxNotifyCount - 1,
                    ThirdPartNotifyStatus = NftOrderWebhookStatus.FAIL.ToString(),
                    WebhookTimeLt = lastWebhookTimeLt
                });
            if (pendingData.Data.IsNullOrEmpty()) break;

            lastWebhookTimeLt = pendingData.Data.Min(order => order.WebhookTime);

            Dictionary<Guid, RampOrderIndex> baseOrderDict = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(
                pendingData.Data.Select(nftOrder => nftOrder.Id.ToString()).ToList());

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Data)
            {
                var orderFound = baseOrderDict.TryGetValue(orderDto.Id, out var baseOrder);
                if (!orderFound || baseOrder == null)
                {
                    _logger.LogError("BaseOrder {OrderId} not found ", orderDto.Id);
                    continue;
                }

                callbackResults.Add(_thirdPartNftOrderProcessorFactory
                    .GetProcessor(baseOrder.MerchantName)
                    .NotifyNftReleaseAsync(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            total += handleCount;
            if (handleCount == 0) break;
        }

        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify finish, total:{Total}", total);
    }

    /// <summary>
    ///     Compensate unprocessed order data from ThirdPart webhook.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task HandleUnCompletedNftOrderPayResultRefresh()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: LockKeyPrefix + "HandleUnCompletedNftOrderPayResultRefresh");
        if (handle == null)
        {
            _logger.LogError("HandleUnCompletedNftOrderPayResultRefresh running, skip");
            return;
        }

        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify start");
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

        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify finish, total:{Total}", total);
    }
}