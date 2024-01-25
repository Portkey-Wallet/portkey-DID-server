using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart.Dtos.Order;

public class PendingTreasuryOrderCondition : PagedResultRequestDto
{
    
    public PendingTreasuryOrderCondition() {}
    
    public PendingTreasuryOrderCondition(int skipCount, int maxResultCount)
    {
        base.SkipCount = skipCount;
        base.MaxResultCount = maxResultCount;
    }
    
    public List<string> StatusIn { get; set; }
    public long? LastModifyTimeGtEq { get; set; }
    public long? LastModifyTimeLt { get; set; }
    public long? ExpireTimeGtEq { get; set; }
    public long? ExpireTimeLt { get; set; }
    public List<string> ThirdPartNameIn { get; set; }
    public List<string> ThirdPartOrderId { get; set; }
}