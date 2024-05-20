using System.Threading.Tasks;
using CAServer.Transfer.Dtos;

namespace CAServer.Transfer.Proxy;

public interface IETransferProxyService
{
    Task<AuthTokenDto> GetConnectTokenAsync(AuthTokenRequestDto request);
    Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request);
    Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(GetTokenOptionListRequestDto request);
    Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request);
    Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(GetCalculateDepositRateRequestDto request);
    Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request);

    Task<GetNetworkTokensDto> GetNetworkTokensAsync(GetNetworkTokensRequestDto request);
}