using System.Threading.Tasks;
using CAServer.Security.Dtos;

namespace CAServer.UserSecurityAppService.Provider;

public interface IUserSecurityProvider
{
    public Task<IndexerTransferLimitList> GetTransferLimitListByCaHash(string caHash);

    public Task<IndexerManagerApprovedList> GetManagerApprovedListByCaHash(string caHash, string spender, string symbol,
        long skip, long maxResultCount);
}