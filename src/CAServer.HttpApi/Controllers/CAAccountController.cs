using System.Collections.Generic;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Growth;
using CAServer.Guardian;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Users;

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
    private readonly ICurrentUser _currentUser;
    private readonly IGrowthAppService _growthAppService;

    public CAAccountController(ICAAccountAppService caAccountService, IGuardianAppService guardianAppService,
        ITransactionFeeAppService transactionFeeAppService, ICurrentUser currentUser,
        IGrowthAppService growthAppService)
    {
        _caAccountService = caAccountService;
        _guardianAppService = guardianAppService;
        _transactionFeeAppService = transactionFeeAppService;
        _currentUser = currentUser;
        _growthAppService = growthAppService;
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
    
    [HttpPost("guardianIdentifiers/unset")]
    [Authorize]
    public async Task<bool> UpdateUnsetGuardianIdentifierAsync(
        UpdateGuardianIdentifierDto updateGuardianIdentifierDto)
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        updateGuardianIdentifierDto.UserId = userId;
        return await _guardianAppService.UpdateUnsetGuardianIdentifierAsync(updateGuardianIdentifierDto);
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

    [HttpGet("revoke/entrance")]
    [Authorize]
    public async Task<RevokeEntranceResultDto> RevokeEntranceAsync()
    {
        return await _caAccountService.RevokeEntranceAsync();
    }

    [HttpGet("revoke/check")]
    [Authorize]
    public async Task<CancelCheckResultDto> CancelCheckAsync()
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        return await _caAccountService.RevokeCheckAsync(userId);
    }

    [HttpPost("revoke/request"), Authorize, IgnoreAntiforgeryToken]
    public async Task<RevokeResultDto> RevokeAsync(RevokeDto input)
    {
        return await _caAccountService.RevokeAsync(input);
    }

    [HttpGet("checkManagerCount")]
    public async Task<CheckManagerCountResultDto> CheckManagerCountAsync(string caHash)
    {
        return await _caAccountService.CheckManagerCountAsync(caHash);
    }

    [HttpGet("{shortLinkCode}")]
    public async Task<IActionResult> GetRedirectUrlAsync(string shortLinkCode)
    {
        var url = await _growthAppService.GetRedirectUrlAsync(shortLinkCode);
        return Redirect(url);
    }

    [HttpPost("revoke/account"), Authorize, IgnoreAntiforgeryToken]
    public async Task<RevokeResultDto> RevokeAccountAsync(RevokeAccountInput input)
    {
        return await _caAccountService.RevokeAccountAsync(input);
    }
    
    [HttpGet("revoke/validate")]
    [Authorize]
    public async Task<CancelCheckResultDto> RevokeValidateAsync(string type)
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        return await _caAccountService.RevokeValidateAsync(userId, type);
    }
    
    [HttpGet("verify/caHolderExist")]
    public async Task<CAHolderExistsResponseDto> CaHolderExistByAddress(string address)
    {
        return await _caAccountService.VerifyCaHolderExistByAddressAsync(address);
    }
    
}