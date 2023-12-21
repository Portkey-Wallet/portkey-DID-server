using System.Collections.Generic;
using CAServer.Guardian;

namespace CAServer.Security.Dtos;

public class TransferLimitListResultDto
{
    public long TotalRecordCount { get; set; }
    public List<TransferLimitDto> Data { get; set; }
}

public class ManagerApprovedListResultDto
{
    public long TotalRecordCount { get; set; }
    public List<ManagerApprovedDto> Data { get; set; }
}

public class TokenBalanceTransferCheckAsyncResultDto
{
    public bool IsTransferSafe { get; set; } = true;
    public bool IsSynchronizing { get; set; }
    public bool IsOriginChainSafe { get; set; } = true;
    
    public List<GuardianIndexerInfoDto> AccelerateGuardians { get; set; } = new();
}