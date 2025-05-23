using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.Security.Dtos;

public class ManagerApprovedDto : ChainDisplayNameDto
{
    public string CaHash { get; set; }
    public string Spender { get; set; }
    public string Symbol { get; set; }
    public long Amount { get; set; }
    public long BlockHeight { get; set; }
}

public class IndexerManagerApprovedList
{
    public CaHolderManagerApproved CaHolderManagerApproved { get; set; }
}

public class CaHolderManagerApproved
{
    public long TotalRecordCount { get; set; }
    public List<ManagerApprovedDto> Data { get; set; }
}