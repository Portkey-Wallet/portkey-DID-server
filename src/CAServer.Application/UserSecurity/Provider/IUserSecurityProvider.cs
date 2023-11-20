using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Security.Dtos;

namespace CAServer.UserSecurity.Provider;

public interface IUserSecurityProvider
{
    public Task<IndexerTransferLimitList> GetTransferLimitListByCaHash(string caHash);

    public Task<IndexerManagerApprovedList> GetManagerApprovedListByCaHash(string caHash, string spender, string symbol,
        long skip, long maxResultCount);
    public Task<UserTransferLimitHistoryIndex> GetUserTransferLimitHistory(string caHash, string chainId, string symbol);

    Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0, int maxResultCount = 10);
}