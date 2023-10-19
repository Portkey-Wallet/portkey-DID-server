using System.Collections.Generic;
using CAServer.Guardian;

namespace CAServer.CAAccount.Provider;

public class GuardianAddedCAHolderDto
{
  public  GuardianAddedHolderInfo GuardianAddedCAHolderInfo { get; set; }
}

public class GuardianAddedHolderInfo
{
    public List<GuardianDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}