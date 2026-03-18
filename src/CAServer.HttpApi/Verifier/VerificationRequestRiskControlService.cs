using System;
using System.Net;
using System.Threading.Tasks;
using CAServer.CAAccount;
using CAServer.CAAccount.Cmd;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.IpInfo;
using CAServer.IpWhiteList;
using CAServer.Switch;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Users;

namespace CAServer.Verifier;

public class VerificationRequestRiskControlService : IVerificationRequestRiskControlService, ITransientDependency
{
    private const string GoogleRecaptcha = "GoogleRecaptcha";
    private readonly ICurrentUser _currentUser;
    private readonly IGoogleAppService _googleAppService;
    private readonly IHttpClientIpResolver _clientIpResolver;
    private readonly IIpWhiteListAppService _ipWhiteListAppService;
    private readonly ILogger<VerificationRequestRiskControlService> _logger;
    private readonly ISecondaryEmailAppService _secondaryEmailAppService;
    private readonly ISwitchAppService _switchAppService;
    private readonly IVerifierAppService _verifierAppService;

    public VerificationRequestRiskControlService(ICurrentUser currentUser, IGoogleAppService googleAppService,
        IIpWhiteListAppService ipWhiteListAppService, IHttpClientIpResolver clientIpResolver,
        ILogger<VerificationRequestRiskControlService> logger, ISecondaryEmailAppService secondaryEmailAppService,
        ISwitchAppService switchAppService, IVerifierAppService verifierAppService)
    {
        _currentUser = currentUser;
        _googleAppService = googleAppService;
        _ipWhiteListAppService = ipWhiteListAppService;
        _clientIpResolver = clientIpResolver;
        _logger = logger;
        _secondaryEmailAppService = secondaryEmailAppService;
        _switchAppService = switchAppService;
        _verifierAppService = verifierAppService;
    }

    public async Task<RiskControlExecutionResult<VerifierServerResponse>> HandleGuardianOperationAsync(
        string recaptchaToken, string acToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return RiskControlExecutionResult<VerifierServerResponse>.Handled(new VerifierServerResponse(),
                HttpStatusCode.Unauthorized);
        }

        return await ExecuteAsync(new RiskControlRequest<VerifierServerResponse>
        {
            RecaptchaToken = recaptchaToken,
            AcToken = acToken,
            OperationType = operationType,
            PlatformType = sendVerificationRequestInput.PlatformType,
            SendAsync = () => _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput),
            OnMissingIp = () =>
            {
                _logger.LogDebug("No userIp in header when operation is {operationType}", operationType);
                return RiskControlExecutionResult<VerifierServerResponse>.Handled();
            },
            OnMissingToken = isWhiteListPath =>
            {
                _logger.LogDebug("No token is provided when operation is {operationType}", operationType);
                return RiskControlExecutionResult<VerifierServerResponse>.Handled();
            },
            OnInvalidToken = (isWhiteListPath, isAcTokenFailure) => isAcTokenFailure
                ? RiskControlExecutionResult<VerifierServerResponse>.Handled(new VerifierServerResponse(),
                    HttpStatusCode.Unauthorized)
                : RiskControlExecutionResult<VerifierServerResponse>.Handled()
        });
    }

    public async Task<RiskControlExecutionResult<VerifySecondaryEmailResponse>> HandleSecondaryEmailAsync(
        string recaptchaToken, string acToken, VerifySecondaryEmailCmd cmd)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return RiskControlExecutionResult<VerifySecondaryEmailResponse>.Handled(new VerifySecondaryEmailResponse(),
                HttpStatusCode.Unauthorized);
        }

        return await ExecuteAsync(new RiskControlRequest<VerifySecondaryEmailResponse>
        {
            RecaptchaToken = recaptchaToken,
            AcToken = acToken,
            OperationType = OperationType.SetSecondaryEmail,
            PlatformType = cmd.PlatformType,
            SendAsync = () => _secondaryEmailAppService.VerifySecondaryEmailAsync(cmd),
            OnMissingIp = () => throw new UserFriendlyException("user ip address not exist"),
            OnMissingToken = isWhiteListPath =>
            {
                if (isWhiteListPath)
                {
                    _logger.LogDebug("No token is provided when operation is {operationType}",
                        OperationType.SetSecondaryEmail);
                    return RiskControlExecutionResult<VerifySecondaryEmailResponse>.Handled();
                }

                throw new UserFriendlyException("invalid recaptchaToken and acToken");
            },
            OnInvalidToken = (isWhiteListPath, isAcTokenFailure) =>
            {
                if (isAcTokenFailure || !isWhiteListPath)
                {
                    return RiskControlExecutionResult<VerifySecondaryEmailResponse>.Handled(
                        new VerifySecondaryEmailResponse(), HttpStatusCode.Unauthorized);
                }

                return RiskControlExecutionResult<VerifySecondaryEmailResponse>.Handled();
            }
        });
    }

    private async Task<RiskControlExecutionResult<TResponse>> ExecuteAsync<TResponse>(
        RiskControlRequest<TResponse> request)
    {
        var userIpAddress = _clientIpResolver.GetBestEffortClientIp();
        if (string.IsNullOrWhiteSpace(userIpAddress))
        {
            return request.OnMissingIp();
        }

        var isInWhiteList = await _ipWhiteListAppService.IsInWhiteListAsync(userIpAddress);
        if (isInWhiteList)
        {
            return await ExecuteWhiteListFlowAsync(request, userIpAddress);
        }

        await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        if (string.IsNullOrWhiteSpace(request.RecaptchaToken) && string.IsNullOrWhiteSpace(request.AcToken))
        {
            return request.OnMissingToken(false);
        }

        return await ValidateAndSendAsync(request, false);
    }

    private async Task<RiskControlExecutionResult<TResponse>> ExecuteWhiteListFlowAsync<TResponse>(
        RiskControlRequest<TResponse> request, string userIpAddress)
    {
        var switchStatus = _switchAppService.GetSwitchStatus(GoogleRecaptcha);
        var googleRecaptchaOpen =
            await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress, request.OperationType);
        await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        if (!switchStatus.IsOpen || !googleRecaptchaOpen)
        {
            return RiskControlExecutionResult<TResponse>.Handled(await request.SendAsync());
        }

        if (string.IsNullOrWhiteSpace(request.RecaptchaToken) && string.IsNullOrWhiteSpace(request.AcToken))
        {
            return request.OnMissingToken(true);
        }

        return await ValidateAndSendAsync(request, true);
    }

    private async Task<RiskControlExecutionResult<TResponse>> ValidateAndSendAsync<TResponse>(
        RiskControlRequest<TResponse> request, bool isWhiteListPath)
    {
        var response = await _googleAppService.ValidateTokenAsync(request.RecaptchaToken, request.AcToken,
            request.PlatformType);

        if (!string.IsNullOrWhiteSpace(request.AcToken) && !response.AcValidResult)
        {
            return request.OnInvalidToken(isWhiteListPath, true);
        }

        if (!string.IsNullOrWhiteSpace(request.AcToken) && response.AcValidResult ||
            !string.IsNullOrWhiteSpace(request.RecaptchaToken) && response.RcValidResult)
        {
            return RiskControlExecutionResult<TResponse>.Handled(await request.SendAsync());
        }

        return request.OnInvalidToken(isWhiteListPath, false);
    }

    private sealed class RiskControlRequest<TResponse>
    {
        public string RecaptchaToken { get; init; }

        public string AcToken { get; init; }

        public OperationType OperationType { get; init; }

        public PlatformType PlatformType { get; init; }

        public Func<Task<TResponse>> SendAsync { get; init; }

        public Func<RiskControlExecutionResult<TResponse>> OnMissingIp { get; init; }

        public Func<bool, RiskControlExecutionResult<TResponse>> OnMissingToken { get; init; }

        public Func<bool, bool, RiskControlExecutionResult<TResponse>> OnInvalidToken { get; init; }
    }
}
