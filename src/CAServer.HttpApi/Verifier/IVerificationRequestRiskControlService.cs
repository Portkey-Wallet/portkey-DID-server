using System.Threading.Tasks;
using CAServer.CAAccount.Cmd;
using CAServer.Dtos;

namespace CAServer.Verifier;

public interface IVerificationRequestRiskControlService
{
    Task<RiskControlExecutionResult<VerifierServerResponse>> HandleGuardianOperationAsync(string recaptchaToken,
        string acToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType);

    Task<RiskControlExecutionResult<VerifierServerResponse>> HandleRecoveryOperationAsync(string recaptchaToken,
        string acToken,
        SendVerificationRequestInput sendVerificationRequestInput, OperationType operationType);

    Task<RiskControlExecutionResult<VerifySecondaryEmailResponse>> HandleSecondaryEmailAsync(string recaptchaToken,
        string acToken, VerifySecondaryEmailCmd cmd);
}
