using System.Collections.Generic;

namespace CAServer.Common;

public class AuthErrorMap
{
    public const string DefaultCode = "50000";
    public const string TwitterCancelCode = "40002";
    private static readonly Dictionary<string, string> ErrorMapInfo = new Dictionary<string, string>()
    {
        ["40001"] = "Portkey needs to verify your account info to continue. Please try again.",
        ["40002"] = "Portkey needs to verify your account info to continue. Please try again.",
        ["40003"] = "Verification has reached the daily limit. Please try again after 24 hours.",
        ["50000"] = "Account verification failed. Please try again."
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