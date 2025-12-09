using System.Collections.Generic;
using CAServer.Commons.Etos;
using CAServer.UserAssets;

namespace CAServer.CAActivity.Dtos;

public class TransactionTypeDto : ChainDisplayNameDto
{
    public List<CAAddressInfo> CaAddressInfos { get; set; }

    public List<string> Types { get; set; }
    public long StartHeight { get; set; }
    public long EndHeight { get; set; }
}