using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processors;

public class TransakOrderProcessor : AbstractOrderProcessor
{

    private readonly ILogger<TransakOrderProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly TransakProvider _transakProvider;
    
    public TransakOrderProcessor(IClusterClient clusterClient, ILogger<TransakOrderProcessor> logger,
        IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus,
        IOrderStatusProvider orderStatusProvider, IObjectMapper objectMapper, TransakProvider transakProvider) : base(clusterClient, logger,
        thirdPartOrderProvider, distributedEventBus, orderStatusProvider, objectMapper)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _transakProvider = transakProvider;
    }

    protected override async Task<IThirdPartOrder>  VerifyOrderInputAsync<T>(T iThirdPartOrder)
    {
        if (iThirdPartOrder is not TransakEventRawDataDto dto)
            throw new UserFriendlyException("not TransakEventRawData");

        var accessToken = await _transakProvider.GetAccessTokenWithRetry();
        var eventData = TransakHelper.DecodeJwt(accessToken, dto.Data);
        var eventObj = JsonConvert.DeserializeObject<TransakOrderUpdateEventDto>(eventData, JsonDecodeSettings);
        if (eventObj?.WebhookData == null || eventObj.WebhookData.IsNullOrEmpty() || eventObj.WebhookOrder == null) 
            throw new UserFriendlyException("convert raw data failed");
        return eventObj.WebhookOrder;
    }

    protected override Task<OrderDto> ConvertOrderDtoAsync<T>(T iThirdPartOrder)
    {
        if (iThirdPartOrder is not TransakOrderDto dto)
            throw new UserFriendlyException("not TransakOrderDto");
        return Task.FromResult(_objectMapper.Map<TransakOrderDto, OrderDto>(dto));
    }

    public override OrderStatusType MapperOrderStatus(OrderDto orderDto)
    {
        return TransakHelper.GetOrderStatus(orderDto.Status);
    }

    public override string MerchantName()
    {
        return MerchantNameType.Transak.ToString();
    }

    public override Task UpdateTxHashAsync(TransactionHashDto transactionHashDto)
    {
        throw new NotImplementedException();
    }

    public override async Task<OrderDto> QueryThirdOrderAsync(OrderDto orderDto)
    {
        var orderInfo = await _transakProvider.GetOrderById(orderDto.Id.ToString());
        return _objectMapper.Map<TransakOrderDto, OrderDto>(orderInfo);
    }
}