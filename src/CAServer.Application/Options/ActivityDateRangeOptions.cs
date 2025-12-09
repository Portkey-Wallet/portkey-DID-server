using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityDateRangeOptions
{
    public Dictionary<string, ActivityDateRange> ActivityDateRanges { get; set; }
}

public class ActivityDateRange
{
    public string StartDate { get; set; }
    
    public string EndDate { get; set; }

}