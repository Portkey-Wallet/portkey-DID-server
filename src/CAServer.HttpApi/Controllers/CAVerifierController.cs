using System;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.IpWhiteList;
using CAServer.Switch;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
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
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAVerifierController> _logger;
    private readonly ISwitchAppService _switchAppService;
    private readonly IGoogleAppService _googleAppService;
    private const string GoogleRecaptcha = "GoogleRecaptcha";
    private const string CheckSwitch = "CheckSwitch";
    private const string XForwardedFor = "X-Forwarded-For";
    private readonly ICurrentUser _currentUser;
    private readonly IIpWhiteListAppService _ipWhiteListAppService;


    public CAVerifierController(IVerifierAppService verifierAppService, IObjectMapper objectMapper,
        ILogger<CAVerifierController> logger, ISwitchAppService switchAppService, IGoogleAppService googleAppService,
        ICurrentUser currentUser, IIpWhiteListAppService ipWhiteListAppService)
    {
        _verifierAppService = verifierAppService;
        _objectMapper = objectMapper;
        _logger = logger;
        _switchAppService = switchAppService;
        _googleAppService = googleAppService;
        _currentUser = currentUser;
        _ipWhiteListAppService = ipWhiteListAppService;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest([FromHeader] string recaptchatoken,
        [FromHeader] string version,
        [FromHeader] string acToken,
        VerifierServerInput verifierServerInput)
    {
        var type = verifierServerInput.OperationType;
        ValidateOperationType(type);
        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);

        if (!_switchAppService.GetSwitchStatus(CheckSwitch).IsOpen)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        return type switch
        {
            OperationType.CreateCAHolder => await RegisterSendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput, type, acToken),
            OperationType.SocialRecovery => await RecoverySendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput, type, acToken),
            _ => await GuardianOperationsSendVerificationRequestAsync(recaptchatoken, sendVerificationRequestInput,
                type, acToken)
        };
    }

    private async Task<VerifierServerResponse> GuardianOperationsSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType, string acToken)
    {
        if (_currentUser.IsAuthenticated)
        {
            return await CheckUserIpAndGoogleRecaptchaAsync(recaptchaToken, sendVerificationRequestInput,
                operationType, acToken);
        }

        HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return new VerifierServerResponse();
    }

    private async Task<VerifierServerResponse> CheckUserIpAndGoogleRecaptchaAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType, string acToken)
    {
        var userIpAddress = UserIpAddress(HttpContext);
        if (string.IsNullOrWhiteSpace(userIpAddress))
        {
            return null;
        }

        var isInWhiteList = await _ipWhiteListAppService.IsInWhiteListAsync(userIpAddress);

        if (isInWhiteList)
        {
            return await GoogleRecaptchaAndSendVerifyCodeAsync(recaptchaToken, sendVerificationRequestInput,
                operationType, acToken);
        }

        await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        if (string.IsNullOrWhiteSpace(recaptchaToken) && string.IsNullOrWhiteSpace(acToken))
        {
            _logger.LogDebug("No token is provided when operation is {operationType}", operationType);
            return null;
        }

        var response =
            await _googleAppService.ValidateTokenAsync(recaptchaToken, acToken,
                sendVerificationRequestInput.PlatformType);

        if (!string.IsNullOrWhiteSpace(acToken) && !response.AcValidResult)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return new VerifierServerResponse();
        }

        if (!string.IsNullOrWhiteSpace(acToken) && response.AcValidResult ||
            !string.IsNullOrWhiteSpace(recaptchaToken) &&
            response.RcValidResult)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        return null;
    }

    private async Task<VerifierServerResponse> GoogleRecaptchaAndSendVerifyCodeAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType, string acToken)
    {
        var userIpAddress = UserIpAddress(HttpContext);
        if (string.IsNullOrWhiteSpace(userIpAddress))
        {
            _logger.LogDebug("No userIp in header when operation is {operationType}", operationType);
            return null;
        }

        _logger.LogDebug("userIp is {userIp}", userIpAddress);
        var switchStatus = _switchAppService.GetSwitchStatus(GoogleRecaptcha);
        var googleRecaptchaOpen =
            await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress, operationType);
        await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        if (!switchStatus.IsOpen || !googleRecaptchaOpen)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        if (string.IsNullOrWhiteSpace(recaptchaToken) && string.IsNullOrWhiteSpace(acToken))
        {
            _logger.LogDebug("No token is provided when operation is {operationType}", operationType);
            return null;
        }

        var response =
            await _googleAppService.ValidateTokenAsync(recaptchaToken,
                acToken, sendVerificationRequestInput.PlatformType);

        if (!string.IsNullOrWhiteSpace(acToken) && !response.AcValidResult)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return new VerifierServerResponse();
        }

        if (!string.IsNullOrWhiteSpace(acToken) && response.AcValidResult ||
            !string.IsNullOrWhiteSpace(recaptchaToken) &&
            response.RcValidResult)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        return null;
    }

    private async Task<VerifierServerResponse> RecoverySendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType, string acToken)
    {
        
        //check guardian isExists;
        var guardianExists =
            await _verifierAppService.GuardianExistsAsync(sendVerificationRequestInput.GuardianIdentifier);
        if (!guardianExists)
        {
            return null;
        }

        return await CheckUserIpAndGoogleRecaptchaAsync(recaptchaToken, sendVerificationRequestInput, operationType,
            acToken);
    }

    private async Task<VerifierServerResponse> RegisterSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType, string acToken)
    {
        return await GoogleRecaptchaAndSendVerifyCodeAsync(recaptchaToken, sendVerificationRequestInput,
            operationType, acToken);
    }

    [HttpPost("verifyCode")]
    public async Task<VerificationCodeResponse> VerifyCode(VerificationSignatureRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyCodeAsync(requestDto);
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

    [HttpPost("verifyTelegramToken")]
    public async Task<VerificationCodeResponse> VerifyTelegramTokenAsync(VerifyTokenRequestDto requestDto)
    {
        ValidateOperationType(requestDto.OperationType);
        return await _verifierAppService.VerifyTelegramTokenAsync(requestDto);
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

        var userIpAddress = UserIpAddress(HttpContext);
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


    private string UserIpAddress(HttpContext context)
    {
        var isHeadersContainsIps = context.Request.Headers.TryGetValue(XForwardedFor, out var userIpAddress);
        if (isHeadersContainsIps)
        {
            var ipAddressList = context.Request.Headers[XForwardedFor];
            if (!string.IsNullOrWhiteSpace(ipAddressList))
            {
                var ips = ipAddressList.ToString().Split(",");
                if (ips.Length > 0)
                {
                    userIpAddress = ips[0].Trim();
                }

                return userIpAddress;
            }
        }

        userIpAddress = context.Connection.RemoteIpAddress?.ToString();
        return userIpAddress;
    }

    private void ValidateOperationType(OperationType operationType)
    {
        var values = Enum.GetValues(typeof(OperationType)).ToDynamicList();
        if (!values.Contains(operationType) || operationType == OperationType.Unknown)
        {
            throw new UserFriendlyException("OperationType is invalid");
        }
    }
}