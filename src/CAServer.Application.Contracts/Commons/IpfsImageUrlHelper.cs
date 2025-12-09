namespace CAServer.Commons;

public static class IpfsImageUrlHelper
{
    private const string OriginalIpfsPrefix = "ipfs://";
    
    public static string TryGetIpfsImageUrl(string imageUrl, string replacedIpfsPrefix)
    {
        if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith(OriginalIpfsPrefix))
        {
            return replacedIpfsPrefix + imageUrl.Substring(OriginalIpfsPrefix.Length);
        }
        return imageUrl;
    }
}