using System.Collections.Generic;

namespace CAServer.Awaken;

public class TradePairsDto
{
    public long TotalCount { get; set; }
    public List<TradePairsItemDto> Items { get; set; }
}