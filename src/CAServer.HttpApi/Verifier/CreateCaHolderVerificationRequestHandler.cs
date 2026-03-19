using System.Threading.Tasks;
using CAServer.IpInfo;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.Verifier;

public class CreateCaHolderVerificationRequestHandler : RegistrationEmailRateLimitedOperationHandlerBase,
    IVerificationRequestOperationHandler, ITransientDependency
{
    private readonly IVerifierAppService _verifierAppService;

    public CreateCaHolderVerificationRequestHandler(IHttpClientIpResolver clientIpResolver,
        ILogger<CreateCaHolderVerificationRequestHandler> logger,
        IRegistrationEmailRateLimitService registrationEmailRateLimitService,
        IVerifierAppService verifierAppService) : base(clientIpResolver, logger, registrationEmailRateLimitService)
    {
        _verifierAppService = verifierAppService;
    }

    public OperationType OperationType => OperationType.CreateCAHolder;

    public async Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context)
    {
        var rateLimitResult = await TryApplyRateLimitAsync(context);
        if (rateLimitResult.IsHandled)
        {
            return rateLimitResult;
        }

        var response = await _verifierAppService.SendVerificationRequestAsync(context.SendVerificationRequestInput);
        return VerificationRequestOperationResult.Handled(response);
    }
}
