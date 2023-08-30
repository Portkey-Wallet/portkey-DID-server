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
}

public class NftOrderProvider : INftOrderProvider, ISingletonDependency
{
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;

    public NftOrderProvider(IThirdPartOrderProvider thirdPartOrderProvider,
        IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
    }


    public async Task HandleUnCompletedMerchantCallback()
    {
        const int pageSize = 100;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
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
                    _thirdPartOrderProvider.CallBackNftOrderPayResultAsync(orderDto.Id, orderDto.Status));
            }

            // non data in page was handled, stop
            // All data at 'lastModifyTimeLt' may have reached max callback-count.
            var handleCount = (await Task.WhenAll(callbackResults.ToArray())).Sum();
            if (handleCount == 0) break;
        }
    }


    public async Task HandleUnCompletedThirdPartResultNotify()
    {
        const int pageSize = 100;
        var lastModifyTimeLt = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
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
            if (handleCount == 0) break;
        }
    }
}