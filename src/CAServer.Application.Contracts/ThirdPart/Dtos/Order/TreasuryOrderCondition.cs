using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart.Dtos.Order;

public class TreasuryOrderCondition : PagedResultRequestDto
{

    public TreasuryOrderCondition() {}
    
    public TreasuryOrderCondition(int skipCount, int maxResultCount)
    {
        base.SkipCount = skipCount;
        base.MaxResultCount = maxResultCount;
    }
    
    
    public List<Guid> IdIn { get; set; }
    public List<Guid> RampOrderIdIn { get; set; }
    public List<string> ThirdPartIdIn { get; set; }
    public string ThirdPartName { get; set; }
    public string TransferDirection { get; set; }
    public string ToAddress { get; set; }
    public List<string> StatusIn { get; set; }
    public string Crypto { get; set; }
    public string TransactionId { get; set; }
    public List<string> CallBackStatusIn { get; set; }
    public int? CallbackCountGtEq { get; set; }
    public int? CallbackCountLt { get; set; }
    public long? CreateTimeGtEq { get; set; }
    public long? CreateTimeLt { get; set; }
    public long? LastModifyTimeGtEq { get; set; }
    public long? LastModifyTimeLt { get; set; }
    
}