using Microsoft.Extensions.Configuration;

namespace CAServer.Nightingale.Common;

public static class ServiceNameHelper
{
    public static string GetServiceName(IConfiguration? configuration)
    {
        var serverName = configuration?.GetValue<string>("ServiceName");
        if (string.IsNullOrWhiteSpace(serverName))
        {
            serverName = configuration?.GetValue<string>("apollo:AppId", N9EClientConstant.Unknown);
        }
        return serverName ?? N9EClientConstant.Unknown;
    }
}