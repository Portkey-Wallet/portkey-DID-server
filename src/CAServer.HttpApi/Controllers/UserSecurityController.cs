using System;
using System.Threading.Tasks;
using Asp.Versioning;
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

    [HttpGet("balanceCheck")]
    public async Task<TokenBalanceTransferCheckAsyncResultDto> GetTokenBalanceTransferCheckAsync(GetTokenBalanceTransferCheckWithChainIdDto input)
    {
        try
        {
            return await _userSecurityAppService.GetTokenBalanceTransferCheckAsync(input);
        }
        catch (Exception e)
        {
            return new TokenBalanceTransferCheckAsyncResultDto
                { IsTransferSafe = false, IsSynchronizing = false, IsOriginChainSafe = false };
        }
    }
}