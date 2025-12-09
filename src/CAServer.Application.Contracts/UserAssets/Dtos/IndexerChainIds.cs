using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class IndexerChainIds
{
    public List<UserChainInfo> CaHolderManagerInfo { get; set; }
}

public class UserChainInfo
{
    public string ChainId { get; set; }
}