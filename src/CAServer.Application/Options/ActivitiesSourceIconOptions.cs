using System.Collections.Generic;

namespace CAServer.Options;

public class ActivitiesSourceIconOptions
{
    public List<SourceIcon> IconInfos { get; set; }
}

public class SourceIcon
{
    public int Platform { get; set; }
    public string Icon { get; set; }
}