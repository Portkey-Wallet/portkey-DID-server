using System;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.Middleware;

public class GoogleRecaptchaMiddleWare
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ILogger<GoogleRecaptchaMiddleWare> _logger;
    private readonly IVerifierAppService _verifierAppService;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOptions;
    private const string ReCaptchaToken = "recaptchatoken";


    public GoogleRecaptchaMiddleWare(ILogger<GoogleRecaptchaMiddleWare> logger,
        IVerifierAppService verifierAppService, RequestDelegate requestDelegate,
        IOptions<GoogleRecaptchaOptions> googleRecaptchaOption)
    {
        _logger = logger;
        _verifierAppService = verifierAppService;
        _requestDelegate = requestDelegate;
        _googleRecaptchaOptions = googleRecaptchaOption.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!ip.IsNullOrWhiteSpace())
        {
            _logger.LogDebug("Received User's RealIp is {ip}", ip.Split(",")[0]);
        }

        var url = context.Request.Path.ToString();
        if (_googleRecaptchaOptions.RecaptchaUrls.Contains(url))
        {
            var recaptchaToken = context.Request.Headers[ReCaptchaToken];
            if (string.IsNullOrEmpty(recaptchaToken))
            {
                throw new Exception("Google Recaptcha Token is Empty");
            }

            try
            {
                var googleRecaptchaTokenResult =
                    await _verifierAppService.VerifyGoogleRecaptchaTokenAsync(recaptchaToken);
                if (googleRecaptchaTokenResult)
                {
                    await _requestDelegate(context);
                }

                _logger.LogDebug("Google Recaptcha Token Verify Failed");
                throw new Exception("Google Recaptcha Token Verify Failed");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Verify Google Recaptcha Token Error");
                throw;
            }
        }
        await _requestDelegate(context);
    }
}