using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAServer.BackGround.Provider;

public interface INftOrderProvider
{
    Task HandleUnCompletedMerchantCallback();
    Task HandleUnCompletedThirdPartResultNotify();
    Task HandleUnCompletedNftOrderPayResultNotify();
}

public class NftOrderProvider : INftOrderProvider, ISingletonDependency
{
    private readonly ILogger<NftOrderProvider> _logger;
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderProvider(IThirdPartOrderProvider thirdPartOrderProvider,
        IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory, ILogger<NftOrderProvider> logger,
        IOrderStatusProvider orderStatusProvider)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
    }


    /// <summary>
    ///     Compensate for NFT-order-pay-result not properly notified to Merchant.
    /// </summary>
    public async Task HandleUnCompletedMerchantCallback()
    {
        _logger.LogInformation("HandleUnCompletedMerchantCallback start");
        const int pageSize = 100;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
        var total = 0;
        while (true)
        {
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeLt = lastModifyTimeLt,
                    StatusIn = new List<string> { OrderStatusType.Pending.ToString() },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Data.IsNullOrEmpty()) break;

            lastModifyTimeLt = pendingData.Data.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<int>>();
            foreach (var orderDto in pendingData.Data)
            {
                callbackResults.Add(
                    _orderStatusProvider.CallBackNftOrderPayResultAsync(orderDto.Id, orderDto.Status));
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
    public async Task HandleUnCompletedThirdPartResultNotify()
    {
        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify start");
        const int pageSize = 100;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
        var total = 0;
        while (true)
        {
            var pendingData = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(
                new GetThirdPartOrderConditionDto(0, pageSize)
                {
                    LastModifyTimeLt = lastModifyTimeLt,
                    StatusIn = new List<string>
                    {
                        OrderStatusType.Finish.ToString(),
                        OrderStatusType.TransferFailed.ToString(),
                    },
                    TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() }
                }, OrderSectionEnum.NftSection);
            if (pendingData.Data.IsNullOrEmpty()) break;

            lastModifyTimeLt = pendingData.Data.Min(order => order.LastModifyTime);

            var callbackResults = new List<Task<CommonResponseDto<Empty>>>();
            foreach (var orderDto in pendingData.Data)
            {
                callbackResults.Add(_thirdPartOrderProcessorFactory.GetProcessor(orderDto.MerchantName)
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
    public async Task HandleUnCompletedNftOrderPayResultNotify()
    {
        _logger.LogInformation("HandleUnCompletedThirdPartResultNotify start");
        const int pageSize = 100;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
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
                callbackResults.Add(_thirdPartOrderProcessorFactory
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