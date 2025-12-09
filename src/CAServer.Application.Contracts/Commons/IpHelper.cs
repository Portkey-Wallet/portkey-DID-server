using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace CAServer.Commons;

public static class IpHelper
{
    
    /*
     IpHelper.AssertAllowed("192.1.1.1", new string[]
       {
           "192.168.1.1",
           "192.168.2.*",
           "10.0.0.1-10.0.0.100"
       })
     */
    public static bool AssertAllowed(string ip, string[] whiteList)
    {
        return whiteList.Any(item => IsMatch(ip, item));
    }
    
    private static bool IsMatch(string ip, string whitelistItem)
    {
        // Check common IP address
        if (whitelistItem == ip)
        {
            return true;
        }

        // Check for wildcards
        if (whitelistItem.Contains('*'))
        {
            var regex = "^" + Regex.Escape(whitelistItem).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(ip, regex);
        }

        // Check IP Segment
        if (whitelistItem.Contains('-'))
        {
            var parts = whitelistItem.Split('-');
            var start = IpAddressToLong(IPAddress.Parse(parts[0]));
            var end = IpAddressToLong(IPAddress.Parse(parts[1]));
            var address = IpAddressToLong(IPAddress.Parse(ip));

            return address >= start && address <= end;
        }

        return false;
    }

    private static long IpAddressToLong(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
    }
}