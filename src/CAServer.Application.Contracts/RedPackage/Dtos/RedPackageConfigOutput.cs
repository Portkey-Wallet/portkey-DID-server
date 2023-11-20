using System.Collections.Generic;

namespace CAServer.RedPackage.Dtos;

public class RedPackageConfigOutput
{
    public Dictionary<string, List<RedPackageTokenInfo>> TokenInfo { get; set; }
}