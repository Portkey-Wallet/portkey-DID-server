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
    private readonly RequestDelegate _next;
    private readonly ILogger<GoogleRecaptchaMiddleWare> _logger;
    private readonly IVerifierAppService _verifierAppService;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOptions;
    private const string ReCaptchaToken = "recaptchatoken";


    public GoogleRecaptchaMiddleWare(ILogger<GoogleRecaptchaMiddleWare> logger,
        IVerifierAppService verifierAppService, RequestDelegate next,
        IOptions<GoogleRecaptchaOptions> googleRecaptchaOption)
    {
        _logger = logger;
        _verifierAppService = verifierAppService;
        _next = next;
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
                context.Response.StatusCode = 500; 
                await context.Response.WriteAsync("Google Recaptcha Token is Empty");
                return;
            }

            try
            {
                var googleRecaptchaTokenResult =
                    await _verifierAppService.VerifyGoogleRecaptchaTokenAsync(recaptchaToken);
                if (googleRecaptchaTokenResult)
                {
                    _logger.LogDebug("Google Recaptcha Token Verify Success");
                    await _next(context);
                }
                _logger.LogDebug("Google Recaptcha Token Verify Failed");
                context.Response.StatusCode = 500; 
                await context.Response.WriteAsync("Google Recaptcha Token is Empty");
                return;
            }
            catch (Exception e)
            {
                _logger.LogDebug("Google Recaptcha Token Verify Failed");
                context.Response.StatusCode = 500; 
                await context.Response.WriteAsync("Google Recaptcha Token is Empty");
                return;
            }
        }
        await _next(context);
    }
}