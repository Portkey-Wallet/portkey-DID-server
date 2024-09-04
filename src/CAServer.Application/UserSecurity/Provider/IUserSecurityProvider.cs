using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Security.Dtos;

namespace CAServer.UserSecurity.Provider;

public interface IUserSecurityProvider
{
    public Task<IndexerTransferLimitList> GetTransferLimitListByCaHashAsync(string caHash);

    public Task<IndexerManagerApprovedList> GetManagerApprovedListByCaHashAsync(string caHash, string spender, string symbol,
        long skip, long maxResultCount);

    Task<IndexerManagerApprovedList> ListManagerApprovedInfoByCaHashAsync(string caHash, string spender,
        string symbol, long skip, long maxResultCount, long startHeight, long endHeight);
    public Task<UserTransferLimitHistoryIndex> GetUserTransferLimitHistoryAsync(string caHash, string chainId, string symbol);

    Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0, int maxResultCount = 10);
}