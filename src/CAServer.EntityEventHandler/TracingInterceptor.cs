using System.Threading.Tasks;
using CAServer.EventBusHandler;
using Serilog.Context;
using Volo.Abp.DynamicProxy;

namespace CAServer.EntityEventHandler;

public class TracingInterceptor : IAbpInterceptor
{
    public async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        if (invocation.Arguments[0] is EtoBase etoBase)
        {
            LogContext.PushProperty("tracing_id", etoBase.TracingId);
        }


        await invocation.ProceedAsync();
    }
}