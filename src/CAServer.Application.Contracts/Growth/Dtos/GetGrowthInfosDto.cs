using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class GetGrowthInfosDto
{
    public long TotalRecordCount { get; set; }
    public List<GrowthUserInfoDto> Data { get; set; }
}