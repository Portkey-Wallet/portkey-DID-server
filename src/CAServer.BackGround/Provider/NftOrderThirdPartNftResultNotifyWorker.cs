using CAServer.BackGround.Consts;
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

public interface INftOrderThirdPartNftResultNotifyWorker
{
    Task Handle();
    
}

public class NftOrderThirdPartNftResultNotifyWorker : INftOrderThirdPartNftResultNotifyWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderThirdPartNftResultNotifyWorker> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;
    private const string LockJobKey = "NftOrderThirdPartNftResultNotifyWorker";

    public NftOrderThirdPartNftResultNotifyWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, ILogger<NftOrderThirdPartNftResultNotifyWorker> logger,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock,
        IOptions<ThirdPartOptions> thirdPartOptions, IOptions<TransactionOptions> transactionOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _nftCheckoutService = nftCheckoutService;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _transactionOptions = transactionOptions.Value;
        _thirdPartOptions = thirdPartOptions.Value;
    }

    /// <summary>
    ///     Compensate for NFT-release-result not properly notified to ThirdPart.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task Handle()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix + LockJobKey);
        if (handle == null)
        {
            _logger.LogWarning("NftOrderThirdPartNftResultNotifyWorker running, skip");
            return;
        }

        _logger.LogDebug("NftOrderThirdPartNftResultNotifyWorker start");
        var maxNotifyCount = _thirdPartOptions.Timer.NftCheckoutResultThirdPartNotifyCount;

        // query ThirdPartNotifyCount > 0 but status is FAIL data
        // when ThirdPartNotifyCount > 0, WebhookTimeLt mast be exists
        var minusAgo = _thirdPartOptions.Timer.NftUnCompletedThirdPartCallbackMinuteAgo;
        var lastWebhookTimeLt = DateTime.UtcNow.AddMinutes(-minusAgo).ToUtcString();
        var total = 0;
        while (true)
        {
            var pendingData = await _thirdPartOrderProvider.QueryNftOrderPagerAsync(
                new NftOrderQueryConditionDto(0, BackGroundConsts.pageSize)
                {
                    ThirdPartNotifyCountGtEq =  BackGroundConsts.minNotifyCount,
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

                callbackResults.Add(_nftCheckoutService
                    .GetProcessor(baseOrder.MerchantName)
                    .NotifyNftReleaseAsync(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            total += handleCount;
            if (handleCount == 0)
            {
                break;
            }
        }

        
        if (total > 0)
        {
            _logger.LogInformation("NftOrderThirdPartNftResultNotifyWorker finish, total:{Total}", total);
        }

    }
}