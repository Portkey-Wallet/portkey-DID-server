using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Guardian;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CARegister")]
[Route("api/app/account/")]
public class CAAccountController : CAServerController
{
    private readonly ICAAccountAppService _caAccountService;
    private readonly IGuardianAppService _guardianAppService;

    public CAAccountController(IGuardianAppService guardianAppService)
    {
        _guardianAppService = guardianAppService;
    }

    [HttpPost("register/request")]
    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        return await _caAccountService.RegisterRequestAsync(input);
    }

    [HttpPost("recovery/request")]
    public async Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input)
    {
        return await _caAccountService.RecoverRequestAsync(input);
    }

    [HttpGet("guardianIdentifiers")]
    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(
        [FromQuery] GuardianIdentifierDto guardianIdentifierDto)
    {
        return await _guardianAppService.GetGuardianIdentifiersAsync(guardianIdentifierDto);
    }

    [HttpGet("registerInfo")]
    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        return await _guardianAppService.GetRegisterInfoAsync(requestDto);
    }
    
    [HttpGet("search")]
    public async Task<SearchResponsePageDto> SearchAsync()
    {
        return await _guardianAppService.SearchAsync();
    }
}