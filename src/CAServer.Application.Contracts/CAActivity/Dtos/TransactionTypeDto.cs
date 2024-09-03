using System.Collections.Generic;

namespace CAServer.CAActivity.Dtos;

public class TransactionTypeDto
{
    public List<string> Types { get; set; }
    public long StartHeight { get; set; }
    public long EndHeight { get; set; }
}