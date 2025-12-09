using System.Collections.Generic;

namespace CAServer.Options;

public class UserGuideInfoOptions
{
    public List<GuideInfo> GuideInfos { get; set; } = new();
}

public class GuideInfo
{
    public int GuideType { get; set; }
    public Dictionary<string, string> ExternalMap { get; set; } = new();
}