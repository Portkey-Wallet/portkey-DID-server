using System.Net;

namespace CAServer.Verifier;

public class RiskControlExecutionResult<TResponse>
{
    public bool IsHandled { get; private set; }

    public HttpStatusCode? StatusCode { get; private set; }

    public TResponse Response { get; private set; }

    public static RiskControlExecutionResult<TResponse> NotHandled()
    {
        return new RiskControlExecutionResult<TResponse>
        {
            IsHandled = false
        };
    }

    public static RiskControlExecutionResult<TResponse> Handled(TResponse response = default,
        HttpStatusCode? statusCode = null)
    {
        return new RiskControlExecutionResult<TResponse>
        {
            IsHandled = true,
            Response = response,
            StatusCode = statusCode
        };
    }
}
