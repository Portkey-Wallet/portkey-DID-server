using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityTypeOptions
{
    public Dictionary<string, string> TypeMap { get; set; }
    public List<string> DefaultTypes { get; set; }
    public HashSet<string> AllSupportTypes { get; set; }
    public List<string> TransferTypes { get; set; }
    public List<string> ContractTypes { get; set; }
    public List<string> ShowPriceTypes { get; set; }
    public List<string> ShowNftTypes { get; set; }
    public List<string> RecentTypes { get; set; }
    public string Zero { get; set; }
}