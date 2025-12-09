using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.Growth.Dtos;

public class GetGrowthInfosRequestDto: PagedResultRequestDto
{
    public string ProjectCode { get; set; }
    public List<string> ReferralCodes { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}