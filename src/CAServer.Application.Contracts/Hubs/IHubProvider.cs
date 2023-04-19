using System;
using System.Threading.Tasks;

namespace CAServer.Hubs;

public interface IHubProvider
{
    Task ResponseAsync<T>(HubResponse<T> res, string clientId, string method, bool isFirstTime = true);
    Task ResponseAsync<T>(HubResponseBase<T> res, string clientId, string method);
    Task ResponseAsync(HubResponse<object> res, string clientId, string method, Type type);
}