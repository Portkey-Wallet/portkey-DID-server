using System.Threading.Tasks;
using CAServer.IpInfo;
using CAServer.Switch;
using Volo.Abp.DependencyInjection;

namespace CAServer.Verifier;

public class SocialRecoveryVerificationRequestHandler : RegistrationEmailRateLimitedOperationHandlerBase,
    IVerificationRequestOperationHandler, ITransientDependency
{
    private const string CheckSwitch = "CheckSwitch";
    private readonly IVerificationRequestRiskControlService _guardianOperationRiskControlService;
    private readonly ISwitchAppService _switchAppService;
    private readonly IVerifierAppService _verifierAppService;

    public SocialRecoveryVerificationRequestHandler(IHttpClientIpResolver clientIpResolver,
        Microsoft.Extensions.Logging.ILogger<SocialRecoveryVerificationRequestHandler> logger,
        IRegistrationEmailRateLimitService registrationEmailRateLimitService,
        IVerificationRequestRiskControlService guardianOperationRiskControlService,
        ISwitchAppService switchAppService, IVerifierAppService verifierAppService)
        : base(clientIpResolver, logger, registrationEmailRateLimitService)
    {
        _guardianOperationRiskControlService = guardianOperationRiskControlService;
        _switchAppService = switchAppService;
        _verifierAppService = verifierAppService;
    }

    public OperationType OperationType => OperationType.SocialRecovery;

    public async Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context)
    {
        var policy = GetRateLimitPolicy(context);
        var checkSwitchOpen = _switchAppService.GetSwitchStatus(CheckSwitch).IsOpen;
        if (policy == null)
        {
            if (!checkSwitchOpen)
            {
                var baselineResponse =
                    await _verifierAppService.SendVerificationRequestAsync(context.SendVerificationRequestInput);
                return VerificationRequestOperationResult.Handled(baselineResponse);
            }

            var legacyGuardianExists =
                await _verifierAppService.GuardianExistsAsync(context.SendVerificationRequestInput.GuardianIdentifier);
            if (!legacyGuardianExists)
            {
                return VerificationRequestOperationResult.Handled();
            }

            var legacyResponse = await _guardianOperationRiskControlService.HandleGuardianOperationAsync(
                context.RecaptchaToken, context.AcToken, context.SendVerificationRequestInput, context.OperationType);
            return legacyResponse.IsHandled
                ? VerificationRequestOperationResult.Handled(legacyResponse.Response, legacyResponse.StatusCode)
                : VerificationRequestOperationResult.NotHandled();
        }

        if (policy.RequireGuardianExistsBeforeConsume)
        {
            var guardianExists =
                await _verifierAppService.GuardianExistsAsync(context.SendVerificationRequestInput.GuardianIdentifier);
            if (!guardianExists)
            {
                return VerificationRequestOperationResult.Handled();
            }
        }

        var rateLimitResult = await TryApplyRateLimitAsync(context);
        if (rateLimitResult.IsHandled)
        {
            return rateLimitResult;
        }

        if (!checkSwitchOpen)
        {
            var directResponse = await _verifierAppService.SendVerificationRequestAsync(context.SendVerificationRequestInput);
            return VerificationRequestOperationResult.Handled(directResponse);
        }

        var response = await _guardianOperationRiskControlService.HandleGuardianOperationAsync(context.RecaptchaToken,
            context.AcToken, context.SendVerificationRequestInput, context.OperationType);
        return response.IsHandled
            ? VerificationRequestOperationResult.Handled(response.Response, response.StatusCode)
            : VerificationRequestOperationResult.NotHandled();
    }
}
