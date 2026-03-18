using System;
using System.Diagnostics;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer;
using CAServer.CAAccount;
using CAServer.CAAccount.Cmd;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.IpInfo;
using CAServer.IpWhiteList;
using CAServer.Switch;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAVerifier")]
[Route("api/app/account")]
public class CAVerifierController : CAServerController
{
    private readonly IVerifierAppService _verifierAppService;
    private readonly IHttpClientIpResolver _clientIpResolver;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAVerifierController> _logger;
    private readonly ISwitchAppService _switchAppService;
    private readonly IGoogleAppService _googleAppService;
    private const string GoogleRecaptcha = "GoogleRecaptcha";
    private const string CheckSwitch = "CheckSwitch";
    private readonly ICurrentUser _currentUser;
    private readonly IIpWhiteListAppService _ipWhiteListAppService;
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly ISecondaryEmailAppService _secondaryEmailAppService;
    private readonly IVerificationRequestOperationDispatcher _verificationRequestOperationDispatcher;
    private readonly IVerificationRequestRiskControlService _verificationRequestRiskControlService;

    public CAVerifierController(IVerifierAppService verifierAppService, IHttpClientIpResolver clientIpResolver,
        IObjectMapper objectMapper,
        ILogger<CAVerifierController> logger, ISwitchAppService switchAppService, IGoogleAppService googleAppService,
        ICurrentUser currentUser, IIpWhiteListAppService ipWhiteListAppService,
        IZkLoginProvider zkLoginProvider, ISecondaryEmailAppService secondaryEmailAppService,
        IVerificationRequestOperationDispatcher verificationRequestOperationDispatcher,
        IVerificationRequestRiskControlService verificationRequestRiskControlService)
    {
        _verifierAppService = verifierAppService;
        _clientIpResolver = clientIpResolver;
        _objectMapper = objectMapper;
        _logger = logger;
        _switchAppService = switchAppService;
        _googleAppService = googleAppService;
        _currentUser = currentUser;
        _ipWhiteListAppService = ipWhiteListAppService;
        _zkLoginProvider = zkLoginProvider;
        _secondaryEmailAppService = secondaryEmailAppService;
        _verificationRequestOperationDispatcher = verificationRequestOperationDispatcher;
        _verificationRequestRiskControlService = verificationRequestRiskControlService;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest([FromHeader] string recaptchatoken,
        [FromHeader] string acToken,
        VerifierServerInput verifierServerInput)
    {
        // if (verifierServerInput.OperationType == OperationType.CreateCAHolder)
        // {
        //     throw new UserFriendlyException("Not support email register yet.");
        // }

        var type = verifierServerInput.OperationType;
        ValidateOperationType(type);

        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        var operationResult = await _verificationRequestOperationDispatcher.HandleAsync(
            new VerificationRequestOperationContext(recaptchatoken, acToken, sendVerificationRequestInput,
                HttpContext.TraceIdentifier));
        if (operationResult.IsHandled)
        {
            ApplyOperationResult(operationResult);
            return operationResult.Response;
        }

        if (!_switchAppService.GetSwitchStatus(CheckSwitch).IsOpen)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        var riskControlResult = await _verificationRequestRiskControlService.HandleGuardianOperationAsync(
            recaptchatoken, acToken, sendVerificationRequestInput, type);
        ApplyRiskControlResult(riskControlResult);
        return riskControlResult.Response;
    }

    [HttpPost("verifyCode")]
    public async Task<VerificationCodeResponse> VerifyCode(VerificationSignatureRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyCodeAsync(requestDto);
    }

    [HttpPost("verifiedzk")]
    public async Task<VerifiedZkResponse> VerifiedZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _zkLoginProvider.VerifiedZkLoginAsync(requestDto);
    }

    [HttpPost("verifyGoogleToken")]
    public async Task<VerificationCodeResponse> VerifyGoogleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyGoogleTokenAsync(requestDto);
    }

    [HttpPost("verifyAppleToken")]
    public async Task<VerificationCodeResponse> VerifyAppleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyAppleTokenAsync(requestDto);
    }

    [HttpPost("verifyFacebookToken")]
    public async Task<VerificationCodeResponse> VerifyFacebookTokenAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyFacebookTokenAsync(requestDto);
    }

    [HttpPost("verifyTelegramToken")]
    public async Task<VerificationCodeResponse> VerifyTelegramTokenAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyTelegramTokenAsync(requestDto);
    }

    [HttpPost("verifyTwitterToken")]
    public async Task<VerificationCodeResponse> VerifyTwitterAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyTwitterTokenAsync(requestDto);
    }

    [HttpPost("isGoogleRecaptchaOpen")]
    public async Task<bool> IsGoogleRecaptchaOpen([FromHeader] string version,
        OperationTypeRequestInput operationTypeRequestInput)
    {
        var type = operationTypeRequestInput.OperationType;
        ValidateOperationType(type);
        if (!_switchAppService.GetSwitchStatus(CheckSwitch).IsOpen)
        {
            return false;
        }

        var userIpAddress = _clientIpResolver.GetBestEffortClientIp();
        _logger.LogDebug("UserIp is {userIp},version is {version}", userIpAddress, version);

        var result = await _ipWhiteListAppService.IsInWhiteListAsync(userIpAddress);
        if (!result)
        {
            return true;
        }

        return await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress,
            type);
    }

    [HttpPost("getVerifierServer")]
    public async Task<GetVerifierServerResponse> GetVerifierServerAsync(GetVerifierServerInfoInput input)
    {
        return await _verifierAppService.GetVerifierServerAsync(input.ChainId);
    }

    private void ValidateOperationType(OperationType operationType)
    {
        var values = Enum.GetValues(typeof(OperationType)).ToDynamicList();
        if (!values.Contains(operationType) || operationType == OperationType.Unknown)
        {
            throw new UserFriendlyException("OperationType is invalid");
        }
    }
    
    [HttpPost("secondary/email/verify")]
    [Authorize]
    public async Task<VerifySecondaryEmailResponse> VerifySecondaryEmailAsync([FromHeader] string recaptchatoken,
        [FromHeader] string acToken, VerifySecondaryEmailCmd cmd)
    {
        if (!_currentUser.IsAuthenticated)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return new VerifySecondaryEmailResponse();
        }
        if (!_switchAppService.GetSwitchStatus(CheckSwitch).IsOpen)
        {
            return await _secondaryEmailAppService.VerifySecondaryEmailAsync(cmd);
        }
        var riskControlResult =
            await _verificationRequestRiskControlService.HandleSecondaryEmailAsync(recaptchatoken, acToken, cmd);
        ApplyRiskControlResult(riskControlResult);
        return riskControlResult.Response;
    }
    
    [HttpPost("verifyCode/secondary/email")]
    [Authorize]
    public async Task<VerifySecondaryEmailCodeResponse> VerifySecondaryEmailCodeAsync(VerifySecondaryEmailCodeCmd cmd)
    {
        var userId = _currentUser.Id ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            throw new UserFriendlyException("user not exist");
        }
        return await _secondaryEmailAppService.VerifySecondaryEmailCodeAsync(cmd);
    }

    [HttpGet("secondary/email")]
    [Authorize]
    public async Task<GetSecondaryEmailResponse> GetSecondaryEmailAsync()
    {
        var userId = _currentUser.Id ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            throw new UserFriendlyException("user not exist");
        }
        return await _secondaryEmailAppService.GetSecondaryEmailAsync(userId);
    }
    
    [HttpGet("verifierServers")]
    public async Task<VerifierServersBasicInfoResponse> GetVerifierServerDetailsAsync(string chainId)
    {
        if (chainId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Please input the chainId");
        }

        var sw = new Stopwatch();
        sw.Start();
        var result = await _verifierAppService.GetVerifierServerDetailsAsync(chainId);
        sw.Stop();
        _logger.LogInformation("GetVerifierServerDetailsAsync cost:{0}ms", sw.ElapsedMilliseconds);
        return result;
    }

    private void ApplyOperationResult(VerificationRequestOperationResult operationResult)
    {
        ApplyStatusCode(operationResult.StatusCode);

        if (operationResult.RetryAfterSeconds.HasValue)
        {
            HttpContext.Response.Headers["Retry-After"] = operationResult.RetryAfterSeconds.Value.ToString();
        }
    }

    private void ApplyRiskControlResult<TResponse>(RiskControlExecutionResult<TResponse> riskControlResult)
    {
        if (riskControlResult == null || !riskControlResult.IsHandled)
        {
            return;
        }

        ApplyStatusCode(riskControlResult.StatusCode);
    }

    private void ApplyStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode.HasValue)
        {
            HttpContext.Response.StatusCode = (int)statusCode.Value;
        }
    }
}
