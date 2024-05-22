using System.Threading.Tasks;
using CAServer.Transfer.Dtos;
using CAServer.Transfer.Proxy;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Transfer;

[RemoteService(false), DisableAuditing]
public class TransferAppService : CAServerAppService, ITransferAppService
{
    private readonly IETransferProxyService _eTransferProxyService;

    public TransferAppService(IETransferProxyService eTransferProxyService)
    {
        _eTransferProxyService = eTransferProxyService;
    }

    public async Task<AuthTokenDto> GetConnectTokenAsync(AuthTokenRequestDto request)
    {
        return await _eTransferProxyService.GetConnectTokenAsync(request);
    }

    public async Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request)
    {
        return await _eTransferProxyService.GetTokenListAsync(request);
    }

    public async Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(GetTokenOptionListRequestDto request)
    {
        return await _eTransferProxyService.GetTokenOptionListAsync(request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        return await _eTransferProxyService.GetNetworkListAsync(request);
    }

    public async Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(GetCalculateDepositRateRequestDto request)
    {
        return await _eTransferProxyService.CalculateDepositRateAsync(request);
    }

    public async Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        return await _eTransferProxyService.GetDepositInfoAsync(request);
    }

    public async Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        return await _eTransferProxyService.GetNetworkTokensAsync(request);
    }
}