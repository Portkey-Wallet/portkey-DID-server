using AElf.ExceptionHandler;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Monitor.Interceptor;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DistributedLocking;

namespace CAServer.BackGround.Provider.Treasury;

public class PendingTreasuryOrderWorker : IJobWorker
{
    private readonly IAbpDistributedLock _distributedLock;
    private readonly ILogger<PendingTreasuryOrderWorker> _logger;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ITreasuryProcessorFactory _processorFactory;


    public PendingTreasuryOrderWorker(IAbpDistributedLock distributedLock, ILogger<PendingTreasuryOrderWorker> logger,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, ITreasuryOrderProvider treasuryOrderProvider,
        IContractProvider contractProvider, ITreasuryProcessorFactory processorFactory,
        IThirdPartOrderProvider thirdPartOrderProvider)
    {
        _distributedLock = distributedLock;
        _logger = logger;
        _thirdPartOptions = thirdPartOptions;
        _treasuryOrderProvider = treasuryOrderProvider;
        _processorFactory = processorFactory;
        _thirdPartOrderProvider = thirdPartOrderProvider;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "PendingTreasuryOrderWorker exist error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP0))
    ]
    public async Task HandleAsync()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync("PendingTreasuryOrderWorker");
        if (handle == null)
        {
            _logger.LogWarning("PendingTreasuryOrderWorker running, skip");
            return;
        }

        _logger.LogDebug("PendingTreasuryOrderWorker start");

        var pageSize = _thirdPartOptions.CurrentValue.Timer.TreasuryTxConfirmWorkerPageSize;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds();
        var expireTimeGtEq = DateTime.UtcNow.ToUtcMilliSeconds();
        var total = 0;
        var count = 0;
        while (true)
        {
            var pendingData = await _treasuryOrderProvider.QueryPendingTreasuryOrderAsync(
                new PendingTreasuryOrderCondition(0, pageSize)
                {
                    StatusIn = new List<string>() { OrderStatusType.Pending.ToString() },
                    LastModifyTimeLt = lastModifyTimeLt,
                    ExpireTimeGtEq = expireTimeGtEq
                });
            if (pendingData.Items.IsNullOrEmpty()) break;
            total += pendingData.Items.Count();
            lastModifyTimeLt = pendingData.Items.Min(order => order.LastModifyTime);


            var thirdPartIdDict = pendingData.Items.GroupBy(p => p.ThirdPartName)
                .ToDictionary(g => g.Key, g => g.Select(dto => dto.ThirdPartOrderId).ToList());
            var rampOrderTask = new List<Task<PagedResultDto<OrderDto>>>();
            foreach (var (thirdPartName, thirdPartIds) in thirdPartIdDict)
            {
                rampOrderTask.Add(_thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                    new GetThirdPartOrderConditionDto(0, pendingData.Items.Count)
                    {
                        ThirdPartName = thirdPartName,
                        ThirdPartOrderNoIn = thirdPartIds
                    }));
            }

            var rampOrders = (await Task.WhenAll(rampOrderTask)).SelectMany(pageResult => pageResult.Items)
                .ToDictionary(order =>
                    string.Join(CommonConstant.Underline, order.MerchantName, order.ThirdPartOrderNo));

            var multiTaskList = new List<Task>();
            foreach (var pendingTreasuryOrderDto in pendingData.Items)
            {
                var rampOrderKey = string.Join(CommonConstant.Underline, pendingTreasuryOrderDto.ThirdPartName,
                    pendingTreasuryOrderDto.ThirdPartOrderId);
                var rampOrderExits = rampOrders.TryGetValue(rampOrderKey, out var rampOrder);
                if (!rampOrderExits || rampOrder == null ||
                    rampOrder.Status == OrderStatusType.Initialized.ToString())
                    continue;

                multiTaskList.Add(_processorFactory.Processor(pendingTreasuryOrderDto.ThirdPartName)
                    .HandlePendingTreasuryOrderAsync(rampOrder, pendingTreasuryOrderDto));
            }

            await Task.WhenAll(multiTaskList);
            count += multiTaskList.Count;
        }

        if (total > 0)
        {
            _logger.LogInformation("PendingTreasuryOrderWorker finish, total:{Count}/{Total}", count, total);
        }
        else
        {
            _logger.LogDebug("PendingTreasuryOrderWorker finish, total:{Count}/{Total}", count, total);
        }
    }
}