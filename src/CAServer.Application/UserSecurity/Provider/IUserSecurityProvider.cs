using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Security.Dtos;

namespace CAServer.UserSecurity.Provider;

public interface IUserSecurityProvider
{
    public Task<IndexerTransferLimitList> GetTransferLimitListByCaHash(string caHash);

    public Task<IndexerManagerApprovedList> GetManagerApprovedListByCaHash(string caHash, string spender, string symbol,
        long skip, long maxResultCount);
    public Task<UserTransferLimitHistoryIndex> GetUserTransferLimitHistory(string caHash, string chainId, string symbol);
}