using System.Collections.Generic;

namespace CAServer.RedPackage;

public class RedPackageOptions
{
    /*public string CoverImage { get; set; }
    public string Link { get; set; }*/
    public int MaxCount { get; set; }
    public List<RedPackageTokenInfo> TokenInfo { get; set; }
}

public class RedPackageTokenInfo
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string MinAmount { get; set; }
}