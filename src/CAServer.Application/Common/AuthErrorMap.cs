using System.Collections.Generic;

namespace CAServer.Common;

public class AuthErrorMap
{
    public const string FacebookCancelCode = "40001";
    public const string TwitterCancelCode = "40002";
    public const string TwitterRequestLimitCode = "40003";
    public const string DefaultCode = "50000";

    public static readonly Dictionary<string, string> ErrorMapInfo = new Dictionary<string, string>()
    {
        [FacebookCancelCode] = "Portkey needs to verify your account info to continue. Please try again.",
        [TwitterCancelCode] = "Portkey needs to verify your account info to continue. Please try again.",
        [TwitterRequestLimitCode] = "Verification has reached the daily limit. Please try again after 24 hours.",
        [DefaultCode] = "Account verification failed. Please try again."
    };


   

    public static string GetMessage(string code)
    {
        return ErrorMapInfo.GetOrDefault(!ErrorMapInfo.ContainsKey(code) ? DefaultCode : code);
    }
    
}