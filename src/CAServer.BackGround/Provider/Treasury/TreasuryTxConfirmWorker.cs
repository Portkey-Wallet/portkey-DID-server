using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider.Treasury;

public class TreasuryTxConfirmWorker : IJobWorker
{
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ILogger<TreasuryTxConfirmWorker> _logger;
    private readonly IOptionsMonitor<TransactionOptions> _transactionOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ITreasuryProcessorFactory _processorFactory;


    public TreasuryTxConfirmWorker(IAbpDistributedLock distributedLock, ILogger<TreasuryTxConfirmWorker> logger,
        IOptionsMonitor<TransactionOptions> transactionOptions, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        ITreasuryOrderProvider treasuryOrderProvider, IContractProvider contractProvider,
        ITreasuryProcessorFactory processorFactory)
    {
        _distributedLock = distributedLock;
        _logger = logger;
        _transactionOptions = transactionOptions;
        _thirdPartOptions = thirdPartOptions;
        _treasuryOrderProvider = treasuryOrderProvider;
        _contractProvider = contractProvider;
        _processorFactory = processorFactory;
    }


    public async Task HandleAsync()
    {
        try
        {
            await using var handle =
                await _distributedLock.TryAcquireAsync(_transactionOptions.CurrentValue.LockKeyPrefix +
                                                             "TreasuryTxConfirmWorker");
            if (handle == null)
            {
                _logger.LogWarning("TreasuryTxConfirmWorker running, skip");
                return;
            }

            var chainStatus = await _contractProvider.GetChainStatusAsync(CommonConstant.MainChainId);
            _logger.LogDebug("TreasuryTxConfirmWorker chainHeight={Height} LIB: {LibHeight}",
                chainStatus.BestChainHeight, chainStatus.LastIrreversibleBlockHeight);

            var pageSize = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferPageSize;
            var secondsAgo = _thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferSecondsAgo;
            var lastModifyTimeLt = DateTime.UtcNow.AddSeconds(-secondsAgo).ToUtcMilliSeconds();
            var lastModifyTimeGtEq = DateTime.UtcNow
                .AddMinutes(-_thirdPartOptions.CurrentValue.Timer.HandleUnCompletedSettlementTransferMinuteAgo)
                .ToUtcMilliSeconds();
            var total = 0;
            var count = 0;
            while (true)
            {
                if (lastModifyTimeLt <= lastModifyTimeGtEq) break;
                var pendingData = await _treasuryOrderProvider.QueryOrderAsync(new TreasuryOrderCondition(0, pageSize)
                {
                    LastModifyTimeGtEq = lastModifyTimeGtEq,
                    LastModifyTimeLt = lastModifyTimeLt,
                    TransferDirection = TransferDirectionType.TokenBuy.ToString(),
                    StatusIn = new List<string>
                    {
                        OrderStatusType.Transferring.ToString(),
                        OrderStatusType.Transferred.ToString()
                    },
                });
                if (pendingData.Items.IsNullOrEmpty()) break;
                total += pendingData.Items.Count;
                lastModifyTimeLt = pendingData.Items.Min(order => order.LastModifyTime);

                var multiConfirmTaskList = new List<Task<CommonResponseDto<Empty>>>();
                foreach (var item in pendingData.Items)
                {
                    multiConfirmTaskList.Add(_processorFactory.Processor(item.ThirdPartName)
                        .RefreshTransferMultiConfirmAsync(item.Id, chainStatus.BestChainHeight,
                            chainStatus.LastIrreversibleBlockHeight));
                }

                var multiConfirmResponseList = await Task.WhenAll(multiConfirmTaskList);
                count += multiConfirmResponseList.Count(resp => resp.Success);
            }

            if (total > 0)
            {
                _logger.LogInformation("TreasuryTxConfirmWorker finish, total:{Count}/{Total}", count, total);
            }
            else
            {
                _logger.LogDebug("TreasuryTxConfirmWorker finish, total:{Count}/{Total}", count, total);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TreasuryTxConfirmWorker error");
        }
    }
}