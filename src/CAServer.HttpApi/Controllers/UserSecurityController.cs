using System.Threading.Tasks;
using CAServer.Security;
using CAServer.Security.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Security")]
[Route("api/app/user/security")]
// [Authorize]
public class UserSecurityController : CAServerController
{
    private readonly IUserSecurityAppService _userSecurityAppService;

    public UserSecurityController(IUserSecurityAppService userSecurityAppService)
    {
        _userSecurityAppService = userSecurityAppService;
    }

    [HttpGet("transferLimit")]
    public async Task<TransferLimitListResultDto> GetTransferLimitListByCaHashAsync(
        GetTransferLimitListByCaHashDto input)
    {
        return await _userSecurityAppService.GetTransferLimitListByCaHashAsync(input);
    }

    [HttpGet("managerApproved")]
    public async Task<ManagerApprovedListResultDto> GetManagerApprovedListByCaHashAsync(
        GetManagerApprovedListByCaHashDto input)
    {
        return await _userSecurityAppService.GetManagerApprovedListByCaHashAsync(input);
    }

    [HttpGet("transferThreshold")]
    public async Task<TokenBalanceTransferThresholdResultDto> GetTokenBalanceTransferThresholdAsync()
    {
        return await _userSecurityAppService.GetTokenBalanceTransferThresholdAsync();
    }
}