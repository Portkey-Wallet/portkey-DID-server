using System.Collections.Generic;

namespace CAServer.RedPackage;

public class RedPackageOptions
{
    public string CoverImage { get; set; }
    public string Link { get; set; }
    public List<RedPackageTokenInfo> Token { get; set; }
}

public class RedPackageTokenInfo
{
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public decimal MinAmount { get; set; }
}