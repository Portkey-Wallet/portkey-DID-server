using System.Collections.Generic;

namespace CAServer.Verifier;

public class ThirdPartyMessage
{
    public static Dictionary<string, string> MessageDictionary = new Dictionary<string, string>
    {
        { "Token expires", "50001" },
        { "Invalid token", "50002" },
        { "Request time out", "50003" }
    };
}