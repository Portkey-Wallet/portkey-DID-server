using CAServer.EnumType;
using CAServer.RedDot.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.RedDot;

public interface IRedDotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<RedDotInfo>> GetRedDotInfo(RedDotType redDotType);
    Task<GrainResultDto<RedDotGrainDto>>  SetRedDot(RedDotType redDotType);
}