using System;
using System.Net.Http;

namespace CAServer.Commons;

public static class MonitorHelper
{
    public static string GetRequestUrl(HttpResponseMessage responseMessage)
    {
        try
        {
            if (responseMessage?.RequestMessage == null) return string.Empty;

            var absoluteUri = responseMessage?.RequestMessage?.RequestUri?.AbsoluteUri;
            var absolutePath = responseMessage?.RequestMessage?.RequestUri?.AbsolutePath;

            if (absoluteUri.IsNullOrWhiteSpace()) return string.Empty;

            if (absolutePath.IsNullOrWhiteSpace()) return absoluteUri;
            return absoluteUri.Substring(0, absoluteUri.LastIndexOf(absolutePath));
        }
        catch (Exception e)
        {
            return string.Empty;
        }
    }
}