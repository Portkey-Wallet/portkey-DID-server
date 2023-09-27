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

public interface INftOrderMerchantCallbackWorker
{
    public Task Handle();
}

public class NftOrderMerchantCallbackWorker : INftOrderMerchantCallbackWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderMerchantCallbackWorker> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;

    public NftOrderMerchantCallbackWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<NftOrderMerchantCallbackWorker> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptions<ThirdPartOptions> thirdPartOptions, IOptions<TransactionOptions> transactionOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _transactionOptions = transactionOptions.Value;
        _thirdPartOptions = thirdPartOptions.Value;
    }
    
    /// <summary>
    ///     Compensate for merchant NFT-order-pay-result not properly notified to Merchant.
    /// </summary>
    ///
    [AutomaticRetry(Attempts = 0)]
    public async Task Handle()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix + "HandleUnCompletedMerchantCallback");
        if (handle == null)
        {
            _logger.LogWarning("HandleUnCompletedMerchantCallback running, skip");
            return;
        }

        _logger.LogDebug("HandleUnCompletedMerchantCallback start");
        const int pageSize = 100;
        const int minCallbackCount = 1;
        var maxCallbackCount = _thirdPartOptions.Timer.NftCheckoutMerchantCallbackCount;

        // query and handle WebhookCount > 0, but status is FAIL data
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

        
        if (total > 0)
        {
            _logger.LogInformation("HandleUnCompletedMerchantCallback finish, total:{Total}", total);
        }
        
    }
}