using System;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Switch;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private const string GoogleRecaptcha = "GoogleRecaptcha";

    public CAVerifierController(IVerifierAppService verifierAppService, IObjectMapper objectMapper,
        ILogger<CAVerifierController> logger, ISwitchAppService switchAppService)
    {
        _verifierAppService = verifierAppService;
        _objectMapper = objectMapper;
        _logger = logger;
        _switchAppService = switchAppService;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest([FromHeader] string recaptchatoken,
        VerifierServerInput verifierServerInput)
    {
        var switchStatus = _switchAppService.GetSwitchStatus(GoogleRecaptcha);
        if (switchStatus.IsOpen)
        {
            return await GoogleRecaptchaAndseSendVerificationRequestAsync(recaptchatoken, verifierServerInput);
        }

        var sendVerificationRequestInput =
            _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
    }

    private async Task<VerifierServerResponse> GoogleRecaptchaAndseSendVerificationRequestAsync(string recaptchatoken,
        VerifierServerInput verifierServerInput)
    {
        if (string.IsNullOrWhiteSpace(recaptchatoken))
        {
            _logger.LogDebug("Google Recaptcha Token is Empty");
            return null;
        }

        var googleRecaptchaTokenSuccess = false;
        try
        {
            googleRecaptchaTokenSuccess = await _verifierAppService.VerifyGoogleRecaptchaTokenAsync(recaptchatoken);
        }
        catch (Exception e)
        {
            _logger.LogDebug("Google Recaptcha Token Verify Failed :{reason}", e.Message);
            return null;
        }

        if (googleRecaptchaTokenSuccess)
        {
            _logger.LogDebug("Google Recaptcha Token Verify Success");
            var sendVerificationRequestInput =
                _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
            return await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        }

        _logger.LogDebug("Google Recaptcha Token Verify Failed");
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
}