namespace CAServer.Commons;

public static class HubCacheHelper
{
    private const string HubCachePrefix = "CaServer:Hub";
    private const string TabCachePrefix = "CaServer:TabCommunication";

    public static string GetTabKey(string key) => $"{TabCachePrefix}:{GetFormatKey(key)}";

    public static string GetHubCacheKey(string key) => $"{HubCachePrefix}:{GetFormatKey(key)}";


    private static string GetFormatKey(string key)
    {
        if (key.Contains(CommonConstant.Hyphen) || key.Contains(CommonConstant.Underline))
        {
            key = key.Replace(CommonConstant.Hyphen, CommonConstant.Colon)
                .Replace(CommonConstant.Underline, CommonConstant.Colon);
        }

        return key;
    }
}