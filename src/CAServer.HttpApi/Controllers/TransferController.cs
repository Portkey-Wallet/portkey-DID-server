using System.Threading.Tasks;
using CAServer.AddressBook.Dtos;
using Asp.Versioning;
using CAServer.Transfer;
using CAServer.Transfer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using GetNetworkListDto = CAServer.Transfer.Dtos.GetNetworkListDto;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Transfer")]
[Route("api/app/transfer")]
[Authorize]
public class TransferController : CAServerController
{
    private readonly ITransferAppService _transferAppService;
    private readonly IShiftChainService _shiftChainService;

    public TransferController(ITransferAppService transferAppService, IShiftChainService shiftChainService)
    {
        _transferAppService = transferAppService;
        _shiftChainService = shiftChainService;
    }

    [AllowAnonymous]
    [HttpPost("connect/token")]
    public async Task<AuthTokenDto> GetConnectTokenAsync([FromForm] AuthTokenRequestDto request)
    {
        return await _transferAppService.GetConnectTokenAsync(request);
    }


    [HttpGet("token/list")]
    public async Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request)
    {
        return await _transferAppService.GetTokenListAsync(request);
    }

    [HttpGet("token/option")]
    public async Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(GetTokenOptionListRequestDto request)
    {
        return await _transferAppService.GetTokenOptionListAsync(request);
    }

    [HttpGet("network/list")]
    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        return await _transferAppService.GetNetworkListAsync(request);
    }
    [HttpGet("getReceiveNetworkList")]
    public async Task<ResponseWrapDto<ReceiveNetworkDto>> GetNetworkListBySymbolAsync(GetReceiveNetworkListRequestDto request)
    {
        return await _shiftChainService.GetReceiveNetworkList(request);
    }

    [HttpGet("getSendNetworkList")]
    public async Task<ResponseWrapDto<SendNetworkDto>> GetDestinationList(GetSendNetworkListRequestDto request)
    {
        return await _shiftChainService.GetSendNetworkList(request);
    }

    [HttpGet("deposit/calculator")]
    public async Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(GetCalculateDepositRateRequestDto request)
    {
        return await _transferAppService.CalculateDepositRateAsync(request);
    }

    [HttpGet("deposit/info")]
    public async Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        return await _transferAppService.GetDepositInfoAsync(request);
    }

    [HttpGet("network/tokens")]
    public async Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        return await _transferAppService.GetNetworkTokensAsync(request);
    }
    
    [HttpGet("record/list")]
    public async Task<ResponseWrapDto<PagedResultDto<OrderIndexDto>>> GetRecordListAsync(GetOrderRecordRequestDto request)
    {
        return await _transferAppService.GetRecordListAsync(request);
    }
    
        
    [HttpGet("support"), AllowAnonymous]
    public async Task<GetSupportNetworkDto> GetSupportNetworkListAsync()
    {
        return await _shiftChainService.GetSupportNetworkListAsync();
    }
}