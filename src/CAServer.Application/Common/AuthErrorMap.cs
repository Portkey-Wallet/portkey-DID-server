using System.Collections.Generic;

namespace CAServer.Common;

public class AuthErrorMap
{
    public const string DefaultCode = "50000";
    public const string TwitterCancelCode = "40002";
    private static readonly Dictionary<string, string> ErrorMapInfo = new Dictionary<string, string>()
    {
        ["40001"] = "facebook cancel auth",
        ["40002"] = "twitter cancel auth",
        ["40003"] = "limit rate",
        ["50000"] = "auth fail"
    };

    public static string GetMessage(string code)
    {
        if (!ErrorMapInfo.ContainsKey(code))
        {
            return ErrorMapInfo.GetOrDefault(DefaultCode);
        }
       
        return ErrorMapInfo.GetOrDefault(code);
    }
}