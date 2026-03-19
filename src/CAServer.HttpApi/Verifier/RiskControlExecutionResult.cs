using System.Net;

namespace CAServer.Verifier;

public class RiskControlExecutionResult<TResponse>
{
    public HttpStatusCode? StatusCode { get; private set; }

    public TResponse Response { get; private set; }

    public static RiskControlExecutionResult<TResponse> Handled(TResponse response = default,
        HttpStatusCode? statusCode = null)
    {
        return new RiskControlExecutionResult<TResponse>
        {
            Response = response,
            StatusCode = statusCode
        };
    }
}
