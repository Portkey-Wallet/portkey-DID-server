using System.Collections.Generic;

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
    public bool IsSafe { get; set; } = true;
}