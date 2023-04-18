using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CARegister")]
[Route("api/app/account/")]
public class CAAccountController : CAServerController
{
    private readonly ICAAccountAppService _caAccountService;

    public CAAccountController(ICAAccountAppService caAccountService)
    {
        _caAccountService = caAccountService;
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
}