using System.Collections.Generic;

namespace CAServer.RedPackage;

public class RedPackageOptions
{
    public int MaxCount { get; set; }
    
    public long ExpireTimeMs{ get; set; }
    public List<RedPackageTokenInfo> TokenInfo { get; set; }
}

public class RedPackageTokenInfo
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string MinAmount { get; set; }
}

public class ContractAddressInfo
{
    public string ChainId{ get; set; }
    public string ContractAddress{ get; set; }
}