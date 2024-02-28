using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Medallion.Threading;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler.Treasury;

public class TreasuryTransferRetryHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly ILogger<TreasuryTransferRetryHandler> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;
    private readonly IAbpDistributedLock _distributedLock;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithAElfTypesConverters()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public TreasuryTransferRetryHandler(ILogger<TreasuryTransferRetryHandler> logger,
        IContractProvider contractProvider,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, ITreasuryOrderProvider treasuryOrderProvider,
        IAbpDistributedLock distributedLock)
    {
        _logger = logger;
        _contractProvider = contractProvider;
        _thirdPartOptions = thirdPartOptions;
        _treasuryOrderProvider = treasuryOrderProvider;
        _distributedLock = distributedLock;
    }


    private bool Match(TreasuryOrderEto eventData)
    {
        return eventData?.Data != null &&
               eventData.Data.TransferDirection == TransferDirectionType.TokenBuy.ToString() &&
               eventData.Data.Status == OrderStatusType.TransferFailed.ToString();
    }

    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        if (!Match(eventData)) return;

        var orderDto = eventData.Data;
        try
        {
            AssertHelper.IsTrue(orderDto.Status == OrderStatusType.TransferFailed.ToString(),
                "Order status not TransferFailed");
            AssertHelper.IsTrue(
                orderDto.TxRetryTimes < _thirdPartOptions.CurrentValue.TreasuryOptions.TransferRetryMaxCount,
                "Transaction max retry count exceeded");

            orderDto.Status = OrderStatusType.StartTransfer.ToString();
            orderDto.TxRetryTimes++;
            await _treasuryOrderProvider.DoSaveOrderAsync(orderDto);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("TreasuryTransferRetryHandler failed: {Message}, orderId={OrderId}, status={Status}",
                e.Message, orderDto.Id, orderDto.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TreasuryTransferRetryHandler error, orderId={OrderId}, status={Status}", orderDto.Id,
                orderDto.Status);
        }
    }
}