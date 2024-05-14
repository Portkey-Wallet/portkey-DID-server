using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace CAServer.Monitor.Interceptor;

[Dependency(ServiceLifetime.Transient)]
public class MonitorInterceptor : AbpInterceptor
{
    private readonly Meter _meter;

    public MonitorInterceptor()
    {
        _meter = new Meter("CAServer", "1.0.0");
    }
    
    public override  async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var methodName = invocation.Method.Name;
        var className = invocation.TargetObject.GetType().Name;

        Histogram<long> executionTimeHistogram = _meter.CreateHistogram<long>(
            name: className + "_" + methodName + "_rt",
            description: "Histogram for method execution time",
            unit: "ms"
        );
        
        stopwatch.Start();

        await invocation.ProceedAsync();

        stopwatch.Stop();

        executionTimeHistogram.Record(stopwatch.ElapsedMilliseconds);

        Counter<long> couter = _meter.CreateCounter<long>(
            name: className + "_" + methodName + "_count",
            description: "Counter for method execution times"
        );
        couter.Add(1);
    }
}