using CAServer.CAActivity.Dto;

namespace CAServer.Grains.State;

[GenerateSerializer]
public class ActivityState
{
    [Id(0)]
    public List<GetActivityDto> ActivitiesDtos = new List<GetActivityDto>();
}