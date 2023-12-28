using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CAServer.Hubs;

public interface IHubService : IApplicationService
{
    Task Ping(HubRequestContext context, string content);
    Task<HubResponse<object>> GetResponse(HubRequestContext context);

    Task RegisterClient(string clientId, string connectionId);
    string UnRegisterClient(string connectionId);

    Task SendAllUnreadRes(string clientId);
    Task Ack(string clientId, string requestId);
    Task RequestOrderTransferredAsync(string targetClientId, string orderId);
    Task RequestAchTxAddressAsync(string targetClientId, string orderId);
    Task RequestNFTOrderStatusAsync(string clientId, string orderId);
    Task RequestRampOrderStatus(string clientId, string orderId);
}