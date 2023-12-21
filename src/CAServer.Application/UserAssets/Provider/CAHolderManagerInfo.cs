using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class CAHolderManagerInfo
{
    public List<CAHolderManager> CaHolderManagerInfo { get; set; }
}

public class CAHolderManager
{
    public string OriginChainId { get; set; }
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public List<ManagerHolder> Managers { get; set; }
}

public class ManagerHolder
{
    public string Manager { get; set; }
    public string DeviceString { get; set; }
}