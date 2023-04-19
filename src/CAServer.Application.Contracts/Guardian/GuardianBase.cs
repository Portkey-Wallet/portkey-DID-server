using System.Collections.Generic;

namespace CAServer.Guardian;

public class GuardianBase
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}

public class GuardianBaseListDto
{
    public List<GuardianInfoBase> Guardians { get; set; }
}

public class ManagerInfoDBase
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}

public class GuardianInfoBase
{
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string GuardianIdentifier { get; set; }
    public string VerifierId { get; set; }
    public bool IsLoginGuardian { get; set; }
    public string Type { get; set; }
}