using System;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Google;
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
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAVerifierController> _logger;
    private readonly ISwitchAppService _switchAppService;
    private readonly IGoogleAppService _googleAppService;
    private const string GoogleRecaptcha = "GoogleRecaptcha";
    private const string XForwardedFor = "X-Forwarded-For";
    private readonly ICurrentUser _currentUser;
    private const string CurrentVersion = "v1.2.9";

    public CAVerifierController(IVerifierAppService verifierAppService, IObjectMapper objectMapper,
        ILogger<CAVerifierController> logger, ISwitchAppService switchAppService, IGoogleAppService googleAppService,
        ICurrentUser currentUser)
    {
        _verifierAppService = verifierAppService;
        _objectMapper = objectMapper;
        _logger = logger;
        _switchAppService = switchAppService;
        _googleAppService = googleAppService;
        _currentUser = currentUser;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest([FromHeader] string recaptchatoken,
        [FromHeader] string version,
        VerifierServerInput verifierServerInput)
    {
        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        if (string.IsNullOrWhiteSpace(version) || version != CurrentVersion)
        {
            return await RegisterSendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput);
        }

        var type = verifierServerInput.OperationType;
        return type switch
        {
            OperationType.Register => await RegisterSendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput),
            OperationType.Recovery => await RecoverySendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput),
            OperationType.GuardianOperations => await GuardianOperationsSendVerificationRequestAsync(recaptchatoken,
                sendVerificationRequestInput),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<VerifierServerResponse> GuardianOperationsSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput)
    {
        var isAuthenticated = _currentUser.IsAuthenticated;
        if (!isAuthenticated)
        {
            return null;
        }
        return await GoogleRecaptchaAndSendVerifyCodeAsync(recaptchaToken, sendVerificationRequestInput);
    }

    private async Task<VerifierServerResponse> GoogleRecaptchaAndSendVerifyCodeAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput)
    {
        var userIpAddress = UserIpAddress(HttpContext);
        if (string.IsNullOrWhiteSpace(userIpAddress))
        {
            return null;
        }

        _logger.LogDebug("userIp is {userIp}", userIpAddress);
        var switchStatus = _switchAppService.GetSwitchStatus(GoogleRecaptcha);
        var googleRecaptchaOpen = await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress);
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
        SendVerificationRequestInput sendVerificationRequestInput)
    {
        //check guardian isExists;
        var guardianExists =
            await _verifierAppService.GuardianExistsAsync(sendVerificationRequestInput.GuardianIdentifier);
        if (!guardianExists)
        {
            return null;
        }
        
        return await GoogleRecaptchaAndSendVerifyCodeAsync(recaptchaToken, sendVerificationRequestInput);
    }

    private async Task<VerifierServerResponse> RegisterSendVerificationRequestAsync(string recaptchaToken,
        SendVerificationRequestInput sendVerificationRequestInput)
    {
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            return null;
        }

        var googleRecaptchaTokenSuccess = false;
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

    [HttpPost("verifyCode")]
    public async Task<VerificationCodeResponse> VerifyCode(VerificationSignatureRequestDto requestDto)
    {
        return await _verifierAppService.VerifyCodeAsync(requestDto);
    }

    [HttpPost("verifyGoogleToken")]
    public async Task<VerificationCodeResponse> VerifyGoogleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        return await _verifierAppService.VerifyGoogleTokenAsync(requestDto);
    }

    [HttpPost("verifyAppleToken")]
    public async Task<VerificationCodeResponse> VerifyAppleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        return await _verifierAppService.VerifyAppleTokenAsync(requestDto);
    }

    [HttpPost("isGoogleRecaptchaOpen")]
    public async Task<bool> IsGoogleRecaptchaOpen()
    {
        var userIpAddress = UserIpAddress(HttpContext);
        return await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress);
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
}