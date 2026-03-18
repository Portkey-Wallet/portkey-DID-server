using System;
using System.Threading.Tasks;
using CAServer.IpInfo;
using CAServer.Switch;
using Volo.Abp.DependencyInjection;

namespace CAServer.Verifier;

public class SocialRecoveryVerificationRequestHandler : RegistrationEmailRateLimitedOperationHandlerBase,
    IVerificationRequestOperationHandler, ITransientDependency
{
    private readonly IVerificationRequestRiskControlService _verificationRequestRiskControlService;
    private readonly ISwitchAppService _switchAppService;
    private readonly IVerifierAppService _verifierAppService;

    public SocialRecoveryVerificationRequestHandler(IHttpClientIpResolver clientIpResolver,
        Microsoft.Extensions.Logging.ILogger<SocialRecoveryVerificationRequestHandler> logger,
        IRegistrationEmailRateLimitService registrationEmailRateLimitService,
        IVerificationRequestRiskControlService verificationRequestRiskControlService,
        ISwitchAppService switchAppService, IVerifierAppService verifierAppService)
        : base(clientIpResolver, logger, registrationEmailRateLimitService)
    {
        _verificationRequestRiskControlService = verificationRequestRiskControlService;
        _switchAppService = switchAppService;
        _verifierAppService = verifierAppService;
    }

    public OperationType OperationType => OperationType.SocialRecovery;

    public async Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context)
    {
        var policy = GetRateLimitPolicy(context);
        var checkSwitchOpen = _switchAppService.GetSwitchStatus(VerificationSwitchNames.CheckSwitch).IsOpen;
        if (policy == null)
        {
            if (!checkSwitchOpen)
            {
                return await ContinueRecoveryAsync(context, checkSwitchOpen);
            }

            if (!await GuardianExistsAsync(context))
            {
                return VerificationRequestOperationResult.Handled();
            }

            return await ContinueRecoveryAsync(context, checkSwitchOpen);
        }

        if (policy.RequireGuardianExistsBeforeConsume && !await GuardianExistsAsync(context))
        {
            return VerificationRequestOperationResult.Handled();
        }

        var rateLimitResult = await TryApplyRateLimitAsync(context);
        if (rateLimitResult.IsHandled)
        {
            return rateLimitResult;
        }

        if (!policy.RequireGuardianExistsBeforeConsume && !await GuardianExistsAsync(context))
        {
            return VerificationRequestOperationResult.Handled();
        }

        return await ContinueRecoveryAsync(context, checkSwitchOpen);
    }

    private async Task<bool> GuardianExistsAsync(VerificationRequestOperationContext context)
    {
        return await _verifierAppService.GuardianExistsAsync(context.SendVerificationRequestInput.GuardianIdentifier);
    }

    private async Task<VerificationRequestOperationResult> ContinueRecoveryAsync(
        VerificationRequestOperationContext context, bool checkSwitchOpen)
    {
        if (!checkSwitchOpen)
        {
            var directResponse = await _verifierAppService.SendVerificationRequestAsync(context.SendVerificationRequestInput);
            return VerificationRequestOperationResult.Handled(directResponse);
        }

        var response = await _verificationRequestRiskControlService.HandleRecoveryOperationAsync(context.RecaptchaToken,
            context.AcToken, context.SendVerificationRequestInput, context.OperationType);
        ArgumentNullException.ThrowIfNull(response);
        return VerificationRequestOperationResult.Handled(response.Response, response.StatusCode);
    }
}
