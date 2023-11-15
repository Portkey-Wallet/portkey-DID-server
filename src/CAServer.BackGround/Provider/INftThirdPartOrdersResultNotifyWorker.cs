using CAServer.BackGround.Consts;
using CAServer.BackGround.Options;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Provider;
using Hangfire;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider;
public interface INftThirdPartOrdersResultNotifyWorker
{
    Task Handle();
}

public class NftThirdPartOrdersResultNotifyWorker : INftThirdPartOrdersResultNotifyWorker, ISingletonDependency
{
    private readonly ILogger<NftThirdPartOrdersResultNotifyWorker> _logger;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly INftOrderThirdPartNftResultNotifyWorker _orderThirdPartNftResultNotifyWorker;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private const string LockJobKey = "NftThirdPartOrdersResultNotifyWorker";
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly TransactionOptions _transactionOptions;







    public NftThirdPartOrdersResultNotifyWorker(ILogger<NftThirdPartOrdersResultNotifyWorker> logger,
        IOptions<ThirdPartOptions> thirdPartOptions,
        INftOrderThirdPartNftResultNotifyWorker orderThirdPartNftResultNotifyWorker,
        IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, IAbpDistributedLock distributedLock, 
        IOptions<TransactionOptions> transactionOptions)
    {
        _logger = logger;
        _thirdPartOptions = thirdPartOptions.Value;
        _orderThirdPartNftResultNotifyWorker = orderThirdPartNftResultNotifyWorker;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _nftCheckoutService = nftCheckoutService;
        _distributedLock = distributedLock;
        _transactionOptions = transactionOptions.Value;
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
            _logger.LogWarning("NftThirdPartOrdersResultNotifyWorker running, skip");
            return;
        }

        _logger.LogDebug("NftThirdPartOrdersResultNotifyWorker start");
        var maxNotifyCount = _thirdPartOptions.Timer.NftCheckoutResultThirdPartNotifyCount;

        var minusAgo = _thirdPartOptions.Timer.NftUnCompletedThirdPartCallbackMinuteAgo;
        var lastWebhookTimeLt = DateTime.UtcNow.AddMinutes(-minusAgo).ToUtcString();

        while (true)
        {
            //GetThirdPartOrdersByPageAsync
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, BackGroundConsts.pageSize)
                {
                    MaxResultCount = maxNotifyCount - 1,
                    LastModifyTimeLt = lastWebhookTimeLt
                }, OrderSectionEnum.SettlementSection);
            if (pendingData.Data.IsNullOrEmpty())
            {
                break;
            }

            lastWebhookTimeLt = pendingData.Data.Min(order => order.LastModifyTime);

            foreach (var orderDto in pendingData.Data)
            {
                if (orderDto.OrderSettlementSection == null || orderDto.CryptoAmount.IsNullOrEmpty())
                {
                    try
                    {
                        await _nftCheckoutService.GetProcessor(orderDto.MerchantName)
                            .SaveOrderSettlementAsync(orderDto.Id);
                    }
                    catch (UserFriendlyException e)
                    {
                        _logger.LogWarning(e, "NftThirdPartOrdersResultNotifyWorker compute result fatal, Id={Id}, Status={Status}",
                            orderDto.Id, orderDto.Status);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "NftThirdPartOrdersResultNotifyWorker compute result fatal, Id={Id}, Status={Status}",
                            orderDto.Id, orderDto.Status);
                        throw;
                    }
                }

            }


        }
    }
}