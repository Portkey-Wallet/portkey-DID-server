using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using CAServer.ThirdPart.Transak;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart.Processor.Ramp;

public class TransakOrderProcessor : AbstractRampOrderProcessor
{
    private readonly TransakProvider _transakProvider;

    public TransakOrderProcessor(IClusterClient clusterClient, IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus, IOrderStatusProvider orderStatusProvider,
        TransakProvider transakProvider, IAbpDistributedLock distributedLock) : base(clusterClient,
        thirdPartOrderProvider, distributedEventBus, orderStatusProvider, distributedLock)
    {
        _transakProvider = transakProvider;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Transak.ToString();
    }

    protected override async Task<OrderDto> VerifyOrderInputAsync<T>(T iThirdPartOrder)
    {
        if (iThirdPartOrder is not TransakEventRawDataDto dto)
            throw new UserFriendlyException("Not TransakEventRawData");

        var accessToken = await _transakProvider.GetAccessTokenWithRetry();
        var eventData = TransakHelper.DecodeJwt(dto.Data, accessToken);
        Logger.LogInformation("Transak webhook data decode: {EventData}", eventData);

        var eventObj = JsonConvert.DeserializeObject<TransakOrderUpdateEventDto>(eventData, JsonDecodeSettings);
        AssertHelper.NotEmpty(eventObj?.WebhookData, "WebhookData empty");
        AssertHelper.NotNull(eventObj?.WebhookOrder, "Webhook order null");

        var orderDto = ObjectMapper.Map<TransakOrderDto, OrderDto>(eventObj.WebhookOrder);
        orderDto.MerchantName = ThirdPartName();
        orderDto.Status = TransakHelper.GetOrderStatus(orderDto.Status).ToString();
        return orderDto;
    }


    public override async Task<OrderDto> QueryThirdOrderAsync(OrderDto orderDto)
    {
        var orderInfo = await _transakProvider.GetOrderByIdAsync(orderDto.Id.ToString());
        var transakOrder = ObjectMapper.Map<TransakOrderDto, OrderDto>(orderInfo);
        transakOrder.Status = TransakHelper.GetOrderStatus(transakOrder.Status).ToString();
        return transakOrder;
    }
}