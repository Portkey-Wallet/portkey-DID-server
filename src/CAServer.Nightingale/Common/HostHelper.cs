using System.Net;
using Serilog;

namespace CAServer.Nightingale.Common;

public static class HostHelper
{
    /// <summary>
    /// note: Failure will return "".
    /// </summary>
    /// <returns></returns>
    public static string GetLocalHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "get host name error.");
        }

        return string.Empty;
    }
}