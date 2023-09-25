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

public interface INftOrderUnCompletedTransferWorker
{
    Task Handle();
}

public class NftOrderSettlementTransferWorker : INftOrderUnCompletedTransferWorker, ISingletonDependency
{
    private readonly ILogger<NftOrderSettlementTransferWorker> _logger;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly TransactionOptions _transactionOptions;

    public NftOrderSettlementTransferWorker(IThirdPartOrderProvider thirdPartOrderProvider,
        IThirdPartNftOrderProcessorFactory thirdPartNftOrderProcessorFactory, ILogger<NftOrderSettlementTransferWorker> logger,
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
    ///     fix uncompleted ELF transfer to merchant
    /// </summary>
    public async Task Handle()
    {
        _logger.LogInformation("HandleUnCompletedNftOrderSettlementTransfer start");
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _transactionOptions.LockKeyPrefix + "HandleUnCompletedNftOrderSettlementTransfer");
        if (handle == null)
        {
            _logger.LogError("HandleUnCompletedNftOrderSettlementTransfer running, skip");
            return;
        }
        
        const int pageSize = 100;
        var secondsAgo = _thirdPartOptions.Timer.HandleUnCompletedSettlementTransferSecondsAgo;
        var lastModifyTimeLt = DateTime.UtcNow.AddSeconds(-secondsAgo).ToUtcMilliSeconds().ToString();
        var modifyTimeGt = DateTime.UtcNow.AddHours(-1).ToUtcMilliSeconds().ToString();
        var total = 0;
        while (true)
        {
            if (string.Compare(lastModifyTimeLt, modifyTimeGt, StringComparison.Ordinal) <= 0) break;
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeLt = lastModifyTimeLt,
                    StatusIn = new List<string>
                    {
                        OrderStatusType.Transferring.ToString(),
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
                    .RefreshSettlementTransfer(orderDto.Id));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max notify-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Count(resp => resp.Success);
            total += handleCount;
        }

        _logger.LogInformation("HandleUnCompletedNftOrderSettlementTransfer finish, total:{Total}", total);
        
        
    }
}