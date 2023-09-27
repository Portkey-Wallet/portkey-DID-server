using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class CaHolderQueryDto
{
    public List<CaHolderInfo> CaHolderInfo { get; set; }
}

public class CaHolderInfo : CaHolderInfoBase
{
    public string OriginChainId { get; set; }
}

public class CaHolderInfoBase
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
}