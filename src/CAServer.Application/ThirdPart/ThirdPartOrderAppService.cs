using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public class ThirdPartOrderAppService : CAServerAppService, IThirdPartOrderAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ThirdPartOrderAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;

    public ThirdPartOrderAppService(IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<ThirdPartOrderAppService> logger,
        IObjectMapper objectMapper)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input)
    {
        var orderId = GuidGenerator.Create();
        var orderGrainData = _objectMapper.Map<CreateUserOrderDto, OrderGrainDto>(input);
        orderGrainData.UserId = CurrentUser.GetId();
        _logger.LogInformation($"This third part order {orderId} will be created.");
        orderGrainData.Status = OrderStatusType.Created.ToString();
        orderGrainData.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        var result = await orderGrain.CreateUserOrderAsync(orderGrainData);
        if (!result.Success)
        {
            _logger.LogError($"Create user order fail, order id: {orderId} user id: {orderGrainData.UserId}");
            return new OrderCreatedDto();
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));

        var resp = _objectMapper.Map<OrderGrainDto, OrderCreatedDto>(result.Data);
        resp.Success = true;
        return resp;
    }

    public async Task<OrdersDto> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        var orderList =
            await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(CurrentUser.GetId(), input.SkipCount,
                input.MaxResultCount);
        return new OrdersDto
        {
            TotalRecordCount = orderList.Count,
            Data = orderList
        };
    }
}