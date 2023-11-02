using CAServer.Common;
using Orleans;
using Orleans.Runtime;

namespace CAServer.Silo;

public class OutgoingGrainTracingFilter : IOutgoingGrainCallFilter
{
    private const string TraceIdentifierKey = "X-Dash-TraceIdentifier";

    private const string IngorePrefix = "Orleans.Runtime";

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        var fullName = context.Grain.GetType().FullName;
        if (fullName != null && fullName.StartsWith(IngorePrefix))
        {
            await context.Invoke();
            return;
        }

        var traceId = DashExecutionContext.TraceIdentifier;

        if (string.IsNullOrEmpty(traceId))
        {
            traceId = Guid.NewGuid().ToString();
        }

        RequestContext.Set(TraceIdentifierKey, traceId);

        await context.Invoke();
    }
}