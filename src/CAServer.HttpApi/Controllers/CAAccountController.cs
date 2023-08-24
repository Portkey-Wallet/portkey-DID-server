using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Guardian;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ITransactionFeeAppService _transactionFeeAppService;

    public CAAccountController(ICAAccountAppService caAccountService, IGuardianAppService guardianAppService,
        ITransactionFeeAppService transactionFeeAppService)
    {
        _caAccountService = caAccountService;
        _guardianAppService = guardianAppService;
        _transactionFeeAppService = transactionFeeAppService;
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

    [HttpGet("transactionFee")]
    public List<TransactionFeeResultDto> CalculateFee(TransactionFeeDto input)
    {
        return _transactionFeeAppService.CalculateFee(input);
    }

    [HttpGet("cancel/entrance")]
    [Authorize]
    public async Task<CancelCheckResultDto> CancelEntranceAsync()
    {
        return await _caAccountService.CancelEntranceAsync();
    }

    [HttpGet("cancel/check")]
    [Authorize]
    public async Task<CancelCheckResultDto> CancelCheckAsync(CancelCheckDto input)
    {
        var userId = CurrentUser.Id;
        return await _caAccountService.CancelCheckAsync(userId);
    }

    [HttpPost("revoke/request"), Authorize]
    public async Task<RevokeResultDto> RevokeAsync(RevokeDto input)
    {
        return await _caAccountService.RevokeAsync(input);
    }
}