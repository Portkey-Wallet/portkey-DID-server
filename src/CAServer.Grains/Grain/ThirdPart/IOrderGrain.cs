namespace CAServer.Grains.Grain.ThirdPart;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<OrderGrainDto>> CreateUserOrderAsync(OrderGrainDto input);
    Task<GrainResultDto<OrderGrainDto>> UpdateOrderAsync(OrderGrainDto input);
    Task<GrainResultDto<OrderGrainDto>> GetOrder();
}