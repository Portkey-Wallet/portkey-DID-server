using System.Collections.Generic;
using CAServer.UserExtraInfo;

namespace CAServer.UserGuide.Dtos;

public class UserGuideDto
{
    public List<UserGuideInfo> UserGuideInfos { get; set; } = new();
}

public class UserGuideInfo
{
    public int Status { get; set; }

    public GuideType GuideType { get; set; }

    public Dictionary<string, string> ExternalMap { get; set; }
}