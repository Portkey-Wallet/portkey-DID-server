using System.Threading.Tasks;

namespace CAServer.Hubs;

public interface IHubWithCacheService
{
    Task RegisterClientAsync(string clientId, string connectionId);
    Task UnRegisterClientAsync(string connectionId);
    Task<string> GetConnectionIdAsync(string clientId);
}