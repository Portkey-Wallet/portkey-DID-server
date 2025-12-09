using System.Collections.Generic;

namespace CAServer.Options;

public class NftToFtOptions
{
    public Dictionary<string, NftToFtInfo> NftToFtInfos { get; set; } = new();
}

public class NftToFtInfo
{
    public string Label { get; set; }
    public string ImageUrl { get; set; }
}