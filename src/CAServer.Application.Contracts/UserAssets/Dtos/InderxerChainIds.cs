using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class InderxerChainIds
{
    public List<UserChainInfo> CaHolderManagerInfo { get; set; }
}

public class UserChainInfo
{
    public string ChainId { get; set; }
}