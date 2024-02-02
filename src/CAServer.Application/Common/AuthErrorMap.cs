using System.Collections.Generic;

namespace CAServer.Common;

public class AuthErrorMap
{
    public const string DefaultCode = "50000";
    public const string FacebookCancelCode = "40001";
    public const string TwitterCancelCode = "40002";
    public const string TwitterVerifyErrorCode = "40003";
    public const string FacebookVerifyErrorCode = "40006";

    public static readonly Dictionary<string, string> ErrorMapInfo = new Dictionary<string, string>()
    {
        [FacebookCancelCode] = "40001",
        [TwitterCancelCode] = "40002",
        [TwitterVerifyErrorCode] = "40003",
        ["40004"] = "40004",
        ["40005"] = "40005",
        [FacebookVerifyErrorCode] = "40006",
        ["50000"] = "50000"
    };

    public static string GetMessage(string code)
    {
        return ErrorMapInfo.GetOrDefault(!ErrorMapInfo.ContainsKey(code) ? DefaultCode : code);
    }
    
}