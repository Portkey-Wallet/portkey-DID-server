using System.Collections.Generic;

namespace CAServer.Guardian.Provider;

public class GuardiansDto
{
    public List<GuardianDto> CaHolderInfo { get; set; }
}

public class GuardianInfo
{
    public List<GuardianDto> CaHolderInfo { get; set; }
}

public class GuardianDto : GuardianBase
{
    public string OriginChainId { get; set; }
    public GuardianBaseListDto GuardianList { get; set; }
    public List<ManagerInfoDBase> ManagerInfos { get; set; }
}