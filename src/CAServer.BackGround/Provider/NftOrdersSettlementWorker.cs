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
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider;

public interface INftOrdersSettlementWorker
{
    Task Handle();
}

public class NftOrdersSettlementWorker : INftOrdersSettlementWorker, ISingletonDependency
{
    private const string LockJobKey = "NftOrdersSettlementWorker";

    private readonly ILogger<NftOrdersSettlementWorker> _logger;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly TransactionOptions _transactionOptions;


    public NftOrdersSettlementWorker(ILogger<NftOrdersSettlementWorker> logger,
        IOptions<ThirdPartOptions> thirdPartOptions,
        IThirdPartOrderProvider thirdPartOrderProvider,
        INftCheckoutService nftCheckoutService, IAbpDistributedLock distributedLock,
        IOptions<TransactionOptions> transactionOptions)
    {
        _logger = logger;
        _thirdPartOptions = thirdPartOptions.Value;
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
            _logger.LogWarning("NftOrdersSettlementWorker running, skip");
            return;
        }

        _logger.LogDebug("NftOrdersSettlementWorker start");

        var minusAgo = _thirdPartOptions.Timer.NftUnCompletedOrderSettlementMinuteAgo;
        var lastModifyTimeLt = DateTime.UtcNow.AddMinutes(-minusAgo).ToUtcMilliSeconds().ToString();

        var total = 0;
        var success = 0;
        
        while (true)
        {
            // GetThirdPartOrdersByPageAsync
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, BackGroundConsts.pageSize)
                {
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() },
                    StatusIn = new List<string> { OrderStatusType.Finish.ToString() },
                    LastModifyTimeLt = lastModifyTimeLt,
                    LastModifyTimeGt = DateTime.UtcNow
                        .AddDays(-_thirdPartOptions.Timer.NftUnCompletedOrderSettlementDaysAgo).ToUtcMilliSeconds().ToString()
                }, OrderSectionEnum.SettlementSection);
            if (pendingData.Data.IsNullOrEmpty())
            {
                break;
            }

            lastModifyTimeLt = pendingData.Data.Min(order => order.LastModifyTime);
            
            foreach (var orderDto in pendingData.Data)
            {
                if (orderDto.OrderSettlementSection != null && orderDto.CryptoAmount.NotNullOrEmpty())
                {
                    continue;
                }
                total++;
                try
                {
                    await _nftCheckoutService.GetProcessor(orderDto.MerchantName)
                        .SaveOrderSettlementAsync(orderDto.Id);
                    success ++;
                }
                catch (UserFriendlyException e)
                {
                    _logger.LogWarning(e, "NftOrdersSettlementWorker compute result fatal, Id={Id}, Status={Status}",
                        orderDto.Id, orderDto.Status);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "NftOrdersSettlementWorker compute result fatal, Id={Id}, Status={Status}",
                        orderDto.Id, orderDto.Status);
                    throw;
                }
            }
        }

        if (total > 0)
        {
            _logger.LogInformation("NftOrdersSettlementWorker finish, success:{Success}/{Total}", success, total);
        }
        else
        {
            _logger.Debug("NftOrdersSettlementWorker finish, success:{Success}/{Total}", success, total);
        }
    }
}