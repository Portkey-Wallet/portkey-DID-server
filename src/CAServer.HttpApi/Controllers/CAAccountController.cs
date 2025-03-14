using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Dtos;
using CAServer.Growth;
using CAServer.Guardian;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly IGoogleZkProvider _googleZkProvider;
    private readonly ILogger<CAAccountController> _logger;

    public CAAccountController(ICAAccountAppService caAccountService, IGuardianAppService guardianAppService,
        ITransactionFeeAppService transactionFeeAppService, ICurrentUser currentUser,
        IGrowthAppService growthAppService, IZkLoginProvider zkLoginProvider, IGoogleZkProvider googleZkProvider,
        ILogger<CAAccountController> logger)
    {
        _caAccountService = caAccountService;
        _guardianAppService = guardianAppService;
        _transactionFeeAppService = transactionFeeAppService;
        _currentUser = currentUser;
        _growthAppService = growthAppService;
        _zkLoginProvider = zkLoginProvider;
        _googleZkProvider = googleZkProvider;
        _logger = logger;
    }

    [HttpPost("register/request")]
    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        return await _caAccountService.RegisterRequestAsync(input);
    }

    [HttpPost("recovery/request")]
    public async Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input)
    {
        var sw = new Stopwatch();
        sw.Start();
        var result = await _caAccountService.RecoverRequestAsync(input);
        sw.Stop();
        _logger.LogInformation("controller RecoverRequest cost:{0}ms", sw.ElapsedMilliseconds);
        return result;
    }

    [HttpGet("guardianIdentifiers")]
    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(
        [FromQuery] GuardianIdentifierDto guardianIdentifierDto)
    {
        var sw = new Stopwatch();
        sw.Start();
        var result = await _guardianAppService.GetGuardianIdentifiersWrapperAsync(guardianIdentifierDto);
        sw.Stop();
        _logger.LogInformation("controller GetGuardianIdentifiers cost:{0}ms", sw.ElapsedMilliseconds);
        return result;
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
        var sw = new Stopwatch();
        sw.Start();
        var result = await _guardianAppService.GetRegisterInfoWrapperAsync(requestDto);
        sw.Stop();
        _logger.LogInformation("controller GetRegisterInfo cost:{0}ms", sw.ElapsedMilliseconds);
        return result;
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

    [HttpGet("manager/check")]
    public async Task<ManagerCacheDto> GetManagerCacheInfo(string manager)
    {
        return await _caAccountService.GetManagerFromCache(manager);
    }
    
    [HttpGet("verify/caHolderExist")]
    public async Task<CAHolderExistsResponseDto> CaHolderExistByAddress(string address)
    {
        return await _caAccountService.VerifyCaHolderExistByAddressAsync(address);
    }
    
    [HttpGet("google-auth-redirect")]
    public async Task<IActionResult> GetGoogleAuthRedirectUrlAsync()
    {
        return Redirect(_googleZkProvider.GetGoogleAuthRedirectUrl());
    }

    [HttpPost("update/guardian")]
    [Authorize]
    public async Task<GuardianEto> UpdateGuardianInfoAsync([FromBody] DBGuardianDto guardianDto)
    {
        return await _zkLoginProvider.UpdateGuardianAsync(guardianDto.GuardianIdentifier, guardianDto.Salt,
            guardianDto.IdentifierHash);
    }
    
    [HttpGet("caholders/es")]
    public async Task<CAHolderReponse> GetAllCaHolderWithTotalAsync(int skip, int limit)
    {
        return await _zkLoginProvider.GetAllCaHolderWithTotalAsync(skip, limit);
    }

    [HttpGet("caholders")]
    public async Task<GuardiansAppDto> GetCaHolderInfoAsync(int skip, int limit)
    {
        return await _zkLoginProvider.GetCaHolderInfoAsync(skip, limit);
    }
    
    [HttpPost("single/poseidon")]
    [Authorize]
    public async Task AppendSinglePoseidonAsync([FromBody]AppendSinglePoseidonDto request)
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        await _zkLoginProvider.AppendSinglePoseidonAsync(request);
    }

    [HttpGet("query/guardians")]
    public async Task<List<GuardianIndexDto>> GetGuardians(string identifierHash)
    {
        return await _guardianAppService.GetGuardianListAsync(new List<string>(){identifierHash});
    }
}