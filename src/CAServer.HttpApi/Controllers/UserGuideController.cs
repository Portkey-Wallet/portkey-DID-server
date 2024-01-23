using System.Threading.Tasks;
using CAServer.Security.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Guide")]
[Route("api/app/user/guide")]
public class UserGuideController: CAServerController
{
    
    
    [HttpGet("list")]
    public async Task<TransferLimitListResultDto> ListUserGuideAsync(
        GetTransferLimitListByCaHashDto input)
    {
        return null;
        // return await _userSecurityAppService.GetTransferLimitListByCaHashAsync(input);
    }
    
    [HttpGet("query")]
    public async Task<TransferLimitListResultDto> QueryUserGuideAsync(
        GetTransferLimitListByCaHashDto input)
    {
        return null;
        // return await _userSecurityAppService.GetTransferLimitListByCaHashAsync(input);
    }
    
    [HttpGet("finish")]
    public async Task<TransferLimitListResultDto> FinishUserGuideAsync(
        GetTransferLimitListByCaHashDto input)
    {
        return null;
        // return await _userSecurityAppService.GetTransferLimitListByCaHashAsync(input);
    }

    
}