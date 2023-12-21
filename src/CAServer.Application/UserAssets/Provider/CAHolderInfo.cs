using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class CAHolderInfo
{
    public List<Manager> CaHolderManagerInfo { get; set; }
}

public class Manager
{
    public string OriginChainId { get; set; }
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public List<HolderManager> ManagerInfos { get; set; }
}

public class HolderManager
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}