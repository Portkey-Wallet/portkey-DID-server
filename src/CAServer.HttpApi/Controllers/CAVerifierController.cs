using System;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.Switch;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

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

    public CAVerifierController(IVerifierAppService verifierAppService, IObjectMapper objectMapper,
        ILogger<CAVerifierController> logger, ISwitchAppService switchAppService, IGoogleAppService googleAppService)
    {
        _verifierAppService = verifierAppService;
        _objectMapper = objectMapper;
        _logger = logger;
        _switchAppService = switchAppService;
        _googleAppService = googleAppService;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest([FromHeader] string recaptchatoken,
        VerifierServerInput verifierServerInput)
    {
        var userIpAddress = UserIpAddress(HttpContext);
        _logger.LogDebug("userIp is {userIp}", userIpAddress);
        var switchStatus = _switchAppService.GetSwitchStatus(GoogleRecaptcha);
        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        var googleRecaptchaOpen = await _googleAppService.IsGoogleRecaptchaOpen(userIpAddress);
        
        if (!switchStatus.IsOpen || !googleRecaptchaOpen)
        {
            await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }
        var googleRecaptchaTokenSuccess = false;
        try
        { 
            googleRecaptchaTokenSuccess = await _googleAppService.GoogleRecaptchaTokenSuccessAsync(recaptchatoken);
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
        return await _googleAppService.IsGoogleRecaptchaOpen(userIpAddress);
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