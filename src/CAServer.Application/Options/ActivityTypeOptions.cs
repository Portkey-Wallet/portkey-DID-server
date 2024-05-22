using System.Collections.Generic;

namespace CAServer.Options;

public class ActivityTypeOptions
{
    public Dictionary<string, string> TypeMap { get; set; }
    
    public Dictionary<string, string> TransactionTypeMap { get; set; }
    public List<string> TransferTypes { get; set; }
    public List<string> ContractTypes { get; set; }
    public List<string> ShowPriceTypes { get; set; }
    public List<string> ShowNftTypes { get; set; }
    public List<string> RecentTypes { get; set; }
    public List<string> RedPacketTypes { get; set; }

    public List<string> NoShowTypes { get; set; }
    public List<string> SystemTypes { get; set; }
    public string Zero { get; set; }
    public List<string> MergeTokenBalanceTypes { get; set; }
}