using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Growth;
using CAServer.Guardian;
using CAServer.Monitor.Interceptor;
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
    private readonly Meter _meter;

    public CAAccountController(ICAAccountAppService caAccountService, IGuardianAppService guardianAppService,
        ITransactionFeeAppService transactionFeeAppService, ICurrentUser currentUser,
        IGrowthAppService growthAppService)
    {
        _caAccountService = caAccountService;
        _guardianAppService = guardianAppService;
        _transactionFeeAppService = transactionFeeAppService;
        _currentUser = currentUser;
        _growthAppService = growthAppService;
        _meter = new Meter("CAServer", "1.0.0");
    }

    [HttpPost("register/request")]
    [Monitor]
    public async Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input)
    {
        return await _caAccountService.RegisterRequestAsync(input);
    }

    [HttpPost("recovery/request")]
    [Monitor]
    public async Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input)
    {
        return await _caAccountService.RecoverRequestAsync(input);
    }

    [HttpGet("guardianIdentifiers")]
    [Monitor]
    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(
        [FromQuery] GuardianIdentifierDto guardianIdentifierDto)
    {
        return await _guardianAppService.GetGuardianIdentifiersAsync(guardianIdentifierDto);
    }

    [HttpGet("registerInfo")]
    [Monitor]
    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Histogram<long> executionTimeHistogram = _meter.CreateHistogram<long>(
            name: "CAAccountController" + "_" + "GetRegisterInfoAsync" + "_rt",
            description: "Histogram for method execution time",
            unit: "ms"
        );
        stopwatch.Start();
        RegisterInfoResultDto registerInfoResultDto = await _guardianAppService.GetRegisterInfoAsync(requestDto);
        stopwatch.Stop();
        executionTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
        return registerInfoResultDto;
    }

    [HttpGet("transactionFee")]
    [Monitor]
    public List<TransactionFeeResultDto> CalculateFee(TransactionFeeDto input)
    {
        return _transactionFeeAppService.CalculateFee(input);
    }

    [HttpGet("revoke/entrance")]
    [Authorize]
    [Monitor]
    public async Task<RevokeEntranceResultDto> RevokeEntranceAsync()
    {
        return await _caAccountService.RevokeEntranceAsync();
    }

    [HttpGet("revoke/check")]
    [Authorize]
    [Monitor]
    public async Task<CancelCheckResultDto> CancelCheckAsync()
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        return await _caAccountService.RevokeCheckAsync(userId);
    }

    [HttpPost("revoke/request"), Authorize, IgnoreAntiforgeryToken]
    [Monitor]
    public async Task<RevokeResultDto> RevokeAsync(RevokeDto input)
    {
        return await _caAccountService.RevokeAsync(input);
    }

    [HttpGet("checkManagerCount")]
    [Monitor]
    public async Task<CheckManagerCountResultDto> CheckManagerCountAsync(string caHash)
    {
        return await _caAccountService.CheckManagerCountAsync(caHash);
    }

    [HttpGet("{shortLinkCode}")]
    [Monitor]
    public async Task<IActionResult> GetRedirectUrlAsync(string shortLinkCode)
    {
        var url = await _growthAppService.GetRedirectUrlAsync(shortLinkCode);
        return Redirect(url);
    }

    [HttpPost("revoke/account"), Authorize, IgnoreAntiforgeryToken]
    [Monitor]
    public async Task<RevokeResultDto> RevokeAccountAsync(RevokeAccountInput input)
    {
        return await _caAccountService.RevokeAccountAsync(input);
    }
    
    [HttpGet("revoke/validate")]
    [Authorize]
    [Monitor]
    public async Task<CancelCheckResultDto> RevokeValidateAsync(string type)
    {
        var userId = _currentUser.Id ?? throw new UserFriendlyException("User not found");
        return await _caAccountService.RevokeValidateAsync(userId, type);
    }
    
}