using System.Threading.Tasks;
using CAServer.Security.Dtos;

namespace CAServer.Security;

public interface IUserSecurityAppService
{
    public Task<TransferLimitListResultDto> GetTransferLimitListByCaHashAsync(
        GetTransferLimitListByCaHashDto input);

    public Task<ManagerApprovedListResultDto> GetManagerApprovedListByCaHashAsync(
        GetManagerApprovedListByCaHashDto input);

    public Task<TokenBalanceTransferCheckAsyncResultDto> GetTokenBalanceTransferCheckAsync(
        GetTokenBalanceTransferCheckWithChainIdDto input);
}