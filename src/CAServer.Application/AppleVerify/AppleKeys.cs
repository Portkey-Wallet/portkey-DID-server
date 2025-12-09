using System.Collections.Generic;

namespace CAServer.AppleVerify;

public class AppleKeys
{
    public List<AppleKey> Keys { get; set; }
}

public class AppleKey
{
    public string Kty { get; set; }
    public string Kid { get; set; }
    public string Use { get; set; }
    public string Alg { get; set; }
    public string N { get; set; }
    public string E { get; set; }
}