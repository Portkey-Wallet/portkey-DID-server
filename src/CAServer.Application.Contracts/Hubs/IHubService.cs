using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CAServer.Hubs;

public interface IHubService : IApplicationService
{
    Task PingAsync(HubRequestContext context, string content);
    Task<HubResponse<object>> GetResponseAsync(HubRequestContext context);

    Task RegisterClientAsync(string clientId, string connectionId);
    string UnRegisterClient(string connectionId);

    Task SendAllUnreadResAsync(string clientId);
    Task AckAsync(string clientId, string requestId);

    Task RequestAchTxAddressAsync(string targetClientId, string orderId);
}