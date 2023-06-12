using System.Collections.Generic;

namespace CAServer.Options;

public class EsIndexBlacklistOptions
{
    public List<string> Indexes { get; set; } = new();
}