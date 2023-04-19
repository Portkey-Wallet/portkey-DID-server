using System.Collections.Generic;

namespace CAServer.Guardian;

public class GuardianResultDto : GuardianBase
{
    public GuardianListDto GuardianList { get; set; }
    public List<ManagerInfoDto> ManagerInfos { get; set; }
}

public class GuardianListDto
{
    public List<GuardianDto> Guardians { get; set; }
}

public class ManagerInfoDto : ManagerInfoDBase
{
}

public class GuardianDto : GuardianInfoBase
{
    public string ThirdPartyEmail { get; set; }
    public bool? IsPrivate { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}