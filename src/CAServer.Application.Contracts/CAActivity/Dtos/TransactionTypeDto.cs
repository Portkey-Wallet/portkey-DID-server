using System.Collections.Generic;
using CAServer.UserAssets;

namespace CAServer.CAActivity.Dtos;

public class TransactionTypeDto
{
    public List<CAAddressInfo> CaAddressInfos { get; set; }
    
    public string ChainId { get; set; }
    public List<string> Types { get; set; }
    public long StartHeight { get; set; }
    public long EndHeight { get; set; }
}