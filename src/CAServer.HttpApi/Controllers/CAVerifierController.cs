using System;
using System.Linq.Dynamic.Core;
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
        VerifierServerInput verifierServerInput)
    {
        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        var type = verifierServerInput.OperationType;
        ValidateOperationType(type);
        if (type == OperationType.Unknown)
        {
            type = OperationType.CreateCAHolder;
        }
        return type switch
        {
            OperationType.CreateCAHolder => await RegisterSendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput, type),
            OperationType.SocialRecovery => await RecoverySendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput, type),
            _ => await GuardianOperationsSendVerificationRequestAsync(recaptchatoken, sendVerificationRequestInput,
                type)
        };
    }

    private async Task<VerifierServerResponse> GuardianOperationsSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return null;
        }

        return await CheckUserIpAndGoogleRecaptchaAsync(recaptchaToken, sendVerificationRequestInput, operationType);
    }

    private async Task<VerifierServerResponse> CheckUserIpAndGoogleRecaptchaAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
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
                operationType);
        }

        await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        var googleRecaptchaTokenSuccess = false;
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            return null;
        }

        try
        {
            googleRecaptchaTokenSuccess = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(recaptchaToken);
        }
        catch (Exception e)
        {
            _logger.LogError("GoogleRecaptchaTokenAsync error: {errorMessage}", e.Message);
            return null;
        }

        if (googleRecaptchaTokenSuccess)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        return null;
    }

    private async Task<VerifierServerResponse> GoogleRecaptchaAndSendVerifyCodeAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
    {
        var userIpAddress = UserIpAddress(HttpContext);
        if (string.IsNullOrWhiteSpace(userIpAddress))
        {
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

        var googleRecaptchaTokenSuccess = false;
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            return null;
        }

        try
        {
            googleRecaptchaTokenSuccess = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(recaptchaToken);
        }
        catch (Exception e)
        {
            _logger.LogError("GoogleRecaptchaTokenAsync error: {errorMessage}", e.Message);
            return null;
        }

        if (googleRecaptchaTokenSuccess)
        {
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        return null;
    }

    private async Task<VerifierServerResponse> RecoverySendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
    {
        //check guardian isExists;
        var guardianExists =
            await _verifierAppService.GuardianExistsAsync(sendVerificationRequestInput.GuardianIdentifier);
        if (!guardianExists)
        {
            return null;
        }

        return await CheckUserIpAndGoogleRecaptchaAsync(recaptchaToken, sendVerificationRequestInput, operationType);
    }

    private async Task<VerifierServerResponse> RegisterSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
    {
        return await GoogleRecaptchaAndSendVerifyCodeAsync(recaptchaToken, sendVerificationRequestInput,
            operationType);
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

    [HttpPost("isGoogleRecaptchaOpen")]
    public async Task<bool> IsGoogleRecaptchaOpen([FromHeader] string version, OperationType operationType)
    {
        ValidateOperationType(operationType);
        var userIpAddress = UserIpAddress(HttpContext);
        _logger.LogDebug("UserIp is {userIp},version is {version}", userIpAddress, version);

        var result = await _ipWhiteListAppService.IsInWhiteListAsync(userIpAddress);
        if (!result)
        {
            return true;
        }

        return await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress, operationType);
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
        if (!values.Contains(operationType))
        {
            throw new UserFriendlyException("OperationType is invalid");
        }
    }
}