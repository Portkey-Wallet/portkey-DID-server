using System.Collections.Generic;

namespace CAServer.Guardian;

public class GuardiansAppDto
{
    public List<GuardianAppDto> CaHolderInfo { get; set; }
}

public class GuardianAppDto : GuardianBase
{
    public string OriginChainId { get; set; }
    public GuardianBaseListDto GuardianList { get; set; }
    public List<ManagerInfoDBase> ManagerInfos { get; set; }
}