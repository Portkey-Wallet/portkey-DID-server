using System.Collections.Generic;

namespace CAServer.Options;

public class AwsThumbnailOptions
{
    public string ImBaseUrl { get; set; }

    public string PortKeyBaseUrl { get; set; }

    public string ForestBaseUrl { get; set; }

    public List<string> ExcludedSuffixes { get; set; }
    
    public List<string> BucketList { get; set; }
}