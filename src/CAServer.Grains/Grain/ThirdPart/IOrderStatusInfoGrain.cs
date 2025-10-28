namespace CAServer.Grains.Grain.ThirdPart;

public interface IOrderStatusInfoGrain : IGrainWithStringKey
{
    Task<OrderStatusInfoGrainResultDto> AddOrderStatusInfo(OrderStatusInfoGrainDto grainDto);
}