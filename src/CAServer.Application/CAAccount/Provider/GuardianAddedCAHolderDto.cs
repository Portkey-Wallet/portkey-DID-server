using System.Collections.Generic;
using CAServer.Guardian;

namespace CAServer.CAAccount.Provider;

public class GuardianAddedCAHolderDto
{
  public  GuardianAddedHolderInfo GuardianAddedCAHolderInfo { get; set; }
}

public class GuardianAddedHolderInfo
{
    public List<GuardianResultDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}