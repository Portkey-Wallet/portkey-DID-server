using System.Net;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.IpInfo;
using Microsoft.Extensions.Logging;

namespace CAServer.Verifier;

public abstract class RegistrationEmailRateLimitedOperationHandlerBase
{
    private readonly IHttpClientIpResolver _clientIpResolver;
    private readonly ILogger _logger;
    private readonly IRegistrationEmailRateLimitService _registrationEmailRateLimitService;

    protected RegistrationEmailRateLimitedOperationHandlerBase(IHttpClientIpResolver clientIpResolver,
        ILogger logger, IRegistrationEmailRateLimitService registrationEmailRateLimitService)
    {
        _clientIpResolver = clientIpResolver;
        _logger = logger;
        _registrationEmailRateLimitService = registrationEmailRateLimitService;
    }

    protected RegistrationEmailRateLimitPolicy GetRateLimitPolicy(VerificationRequestOperationContext context)
    {
        return _registrationEmailRateLimitService.GetPolicy(CreateRateLimitContext(context));
    }

    protected async Task<VerificationRequestOperationResult> TryApplyRateLimitAsync(
        VerificationRequestOperationContext context)
    {
        var rateLimitContext = CreateRateLimitContext(context);
        var policy = _registrationEmailRateLimitService.GetPolicy(rateLimitContext);
        if (policy == null)
        {
            return VerificationRequestOperationResult.NotHandled();
        }

        var clientIp = _clientIpResolver.GetForwardedClientIp();
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            _logger.LogWarning(
                "Registration email rate limit rejected request due to missing ip headers. traceId:{TraceId}, operationType:{OperationType}",
                context.TraceId, context.OperationType);
            return VerificationRequestOperationResult.Handled(new VerifierServerResponse(), HttpStatusCode.BadRequest);
        }

        _clientIpResolver.SetResolvedClientIp(clientIp);
        rateLimitContext.ClientIp = clientIp;
        var result = await _registrationEmailRateLimitService.CheckAsync(rateLimitContext);
        if (result.IsAllowed)
        {
            return VerificationRequestOperationResult.NotHandled();
        }

        return VerificationRequestOperationResult.Handled(new VerifierServerResponse(), HttpStatusCode.TooManyRequests,
            result.RetryAfterSeconds);
    }

    private static RegistrationEmailRateLimitContext CreateRateLimitContext(VerificationRequestOperationContext context)
    {
        return new RegistrationEmailRateLimitContext
        {
            GuardianType = context.SendVerificationRequestInput.Type,
            OperationType = context.OperationType,
            TraceId = context.TraceId
        };
    }
}
