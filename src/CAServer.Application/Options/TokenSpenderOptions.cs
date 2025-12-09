using System.Collections.Generic;

namespace CAServer.Options;

public class TokenSpenderOptions
{
    public List<TokenSpender> TokenSpenderList { get; set; } = new();
}

public class TokenSpender
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
    public string Url { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
}