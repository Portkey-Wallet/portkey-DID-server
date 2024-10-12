using AElf.ExceptionHandler;
using CAServer.BackGround.Options;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider.Treasury;

public class TreasuryCallbackWorker : IJobWorker
{
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ILogger<TreasuryCallbackWorker> _logger;
    private readonly IOptionsMonitor<TransactionOptions> _transactionOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ITreasuryProcessorFactory _processorFactory;

    public TreasuryCallbackWorker(IAbpDistributedLock distributedLock, ILogger<TreasuryCallbackWorker> logger,
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
    
    [ExceptionHandler(typeof(Exception),
        Message = "TreasuryCallbackWorker exist error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP0))
    ]
    public async Task HandleAsync()
    {
        await using var handle =
                await _distributedLock.TryAcquireAsync(name: _transactionOptions.CurrentValue.LockKeyPrefix +
                                                             "TreasuryCallbackWorker");
            if (handle == null)
            {
                _logger.LogWarning("TreasuryCallbackWorker running, skip");
                return;
            }

            _logger.LogDebug("TreasuryCallbackWorker start");

            var minCallbackCount = 1;
            var maxCallbackCount = _thirdPartOptions.CurrentValue.Timer.TreasuryCallbackMaxCount;
            var pageSize = _thirdPartOptions.CurrentValue.Timer.TreasuryTxConfirmWorkerPageSize;
            var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds();
            var lastModifyTimeGtEq = DateTime.UtcNow
                .AddMinutes(-_thirdPartOptions.CurrentValue.Timer.TreasuryCallbackFromMinutesAgo)
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
                    CallBackStatusIn = new List<string> { TreasuryCallBackStatus.Failed.ToString()},
                    CallbackCountGtEq = minCallbackCount,
                    CallbackCountLt = maxCallbackCount,
                    TransferDirection = TransferDirectionType.TokenBuy.ToString(),
                });
                if (pendingData.Items.IsNullOrEmpty()) break;
                total += pendingData.Items.Count();
                lastModifyTimeLt = pendingData.Items.Min(order => order.LastModifyTime);

                var multiConfirmTaskList = new List<Task<CommonResponseDto<Empty>>>();
                foreach (var item in pendingData.Items)
                {
                    multiConfirmTaskList.Add(_processorFactory.Processor(item.ThirdPartName)
                        .CallBackAsync(item.Id));
                }

                var multiConfirmResponseList = await Task.WhenAll(multiConfirmTaskList);
                count += multiConfirmResponseList.Count(resp => resp.Success);
            }

            if (total > 0)
            {
                _logger.LogInformation("TreasuryCallbackWorker finish, total:{Count}/{Total}", count, total);
            }
            else
            {
                _logger.LogDebug("TreasuryCallbackWorker finish, total:{Count}/{Total}", count, total);
            }
    }
}