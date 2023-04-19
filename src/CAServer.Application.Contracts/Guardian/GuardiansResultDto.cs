using System.Collections.Generic;
using Portkey.Contracts.CA;

namespace CAServer.Guardian;

public class GuardianResultDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public GuardianListDto GuardianList { get; set; }
    public List<ManagerInfoDto> ManagerInfos { get; set; }
}

public class GuardianListDto
{
    public List<GuardianDto> Guardians { get; set; }
}

public class ManagerInfoDto
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}

public class GuardianDto
{
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string GuardianIdentifier { get; set; }
    public string VerifierId { get; set; }
    public bool IsLoginGuardian { get; set; }
    public string Type { get; set; }
    public string ThirdPartyEmail { get; set; }
    public bool? IsPrivate { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}