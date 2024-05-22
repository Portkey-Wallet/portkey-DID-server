using System.Threading.Tasks;
using CAServer.Tab.Dtos;

namespace CAServer.Hubs;

public interface IHubWithCacheService
{
    Task RegisterClientAsync(string clientId, string connectionId);
    Task UnRegisterClientAsync(string connectionId);
    Task<string> GetConnectionIdAsync(string clientId);
    Task<TabCompleteInfo> GetTabCompleteInfoAsync(string key);
}