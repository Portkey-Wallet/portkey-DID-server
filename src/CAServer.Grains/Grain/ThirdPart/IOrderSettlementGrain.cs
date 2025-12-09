using CAServer.ThirdPart;

namespace CAServer.Grains.Grain.ThirdPart;

public interface IOrderSettlementGrain : IGrainWithGuidKey
{
    
    public Task<GrainResultDto<OrderSettlementGrainDto>> AddUpdate(OrderSettlementGrainDto grainDto);
    
    public Task<GrainResultDto<OrderSettlementGrainDto>> GetById(Guid id);
    
}