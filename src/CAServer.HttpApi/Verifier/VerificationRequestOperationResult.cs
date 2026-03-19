using System.Net;
using CAServer.Dtos;

namespace CAServer.Verifier;

public class VerificationRequestOperationResult
{
    public bool IsHandled { get; private set; }

    public HttpStatusCode? StatusCode { get; private set; }

    public int? RetryAfterSeconds { get; private set; }

    public VerifierServerResponse Response { get; private set; }

    public static VerificationRequestOperationResult NotHandled()
    {
        return new VerificationRequestOperationResult
        {
            IsHandled = false
        };
    }

    public static VerificationRequestOperationResult Handled(VerifierServerResponse response = null,
        HttpStatusCode? statusCode = null, int? retryAfterSeconds = null)
    {
        return new VerificationRequestOperationResult
        {
            IsHandled = true,
            Response = response,
            StatusCode = statusCode,
            RetryAfterSeconds = retryAfterSeconds
        };
    }
}
