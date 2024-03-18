namespace CAServer.Commons;

public static class IpfsImageUrlHelper
{
    private const string OriginalIpfsPrefix = "ipfs://";
    private const string ReplacedIpfsPrefix = "https://ipfs.io/ipfs/";
    
    public static string TryGetIpfsImageUrl(string imageUrl)
    {
        if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith(OriginalIpfsPrefix))
        {
            return ReplacedIpfsPrefix + imageUrl.Substring(OriginalIpfsPrefix.Length);
        }
        return imageUrl;
    }
}