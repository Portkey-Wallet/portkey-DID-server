using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class IndexCaHolderManagerInfo
{
    
    public List<CaHolderManagerInfo> CaHolderManagerInfo { get; set; }
}

public class CaHolderManagerInfo
{
     public string ChainId { get; set; }
    
    public string CaHash { get; set; }
    
     public string CaAddress { get; set; }
}