using System.Threading.Tasks;
using CAServer.Transfer.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.Transfer.Proxy;

public interface IETransferProxyService
{
    Task<AuthTokenDto> GetConnectTokenAsync(AuthTokenRequestDto request);
    Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request);
    Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(GetTokenOptionListRequestDto request);
    Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request);
    Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(GetCalculateDepositRateRequestDto request);
    Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request);

    Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request);
    Task<ResponseWrapDto<PagedResultDto<OrderIndexDto>>> GetRecordListAsync(GetOrderRecordRequestDto request);
}