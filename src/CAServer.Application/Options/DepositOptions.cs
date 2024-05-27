using System.Collections.Generic;

namespace CAServer.Options;

public class DepositOptions
{
    public List<string> ExtraNotes { get; set; }
    public List<string> SwapExtraNotes { get; set; }
    public Dictionary<string, WordReflection> Reflection { get; set; }
}

public class WordReflection
{
    public string Include { get; set; }
    public string NextWord { get; set; }
}