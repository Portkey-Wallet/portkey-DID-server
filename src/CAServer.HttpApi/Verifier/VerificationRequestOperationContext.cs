namespace CAServer.Verifier;

public class VerificationRequestOperationContext
{
    public VerificationRequestOperationContext(string recaptchaToken, string acToken,
        SendVerificationRequestInput sendVerificationRequestInput, string traceId)
    {
        RecaptchaToken = recaptchaToken;
        AcToken = acToken;
        SendVerificationRequestInput = sendVerificationRequestInput;
        TraceId = traceId;
    }

    public string RecaptchaToken { get; }

    public string AcToken { get; }

    public string TraceId { get; }

    public SendVerificationRequestInput SendVerificationRequestInput { get; }

    public OperationType OperationType => SendVerificationRequestInput.OperationType;
}
