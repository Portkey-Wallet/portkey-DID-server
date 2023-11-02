using CAServer.Common;
using Orleans;
using Orleans.Runtime;

namespace CAServer.Silo;

public class IncomingGrainTracingFilter : IIncomingGrainCallFilter
{
    private const string TraceIdentifierKey = "X-Dash-TraceIdentifier";

    private const string IngorePrefix = "Orleans.Runtime";

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var fullName = context.Grain.GetType().FullName;
        if (fullName != null && fullName.StartsWith(IngorePrefix))
        {
            await context.Invoke();
            return;
        }
        DashExecutionContext.TrySetTraceIdentifier(RequestContext.Get(TraceIdentifierKey).ToString());
        await context.Invoke();
    }
}