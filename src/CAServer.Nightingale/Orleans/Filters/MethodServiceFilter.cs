using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace CAServer.Nightingale.Orleans.Filters;

public class MethodServiceFilter : IIncomingGrainCallFilter
{
    private readonly ILogger? _logger;
    private readonly N9EClientFactory? _n9EClientFactory;
    private readonly IOptionsMonitor<MethodServiceFilterOptions> _methodServiceFilterOption;

    private readonly GrainMethodFormatter.GrainMethodFormatterDelegate _methodFormatter =
        GrainMethodFormatter.MethodFormatter;

    public MethodServiceFilter(IServiceProvider serviceProvider,
        IOptionsMonitor<MethodServiceFilterOptions> methodServiceFilterOption)
    {
        _logger = serviceProvider.GetService<ILogger<MethodCallFilter>>();
        _n9EClientFactory = serviceProvider.GetService<N9EClientFactory>();
        _methodServiceFilterOption = methodServiceFilterOption;
        var formatterDelegate = serviceProvider.GetService<GrainMethodFormatter.GrainMethodFormatterDelegate>();
        if (formatterDelegate != null)
        {
            _methodFormatter = formatterDelegate;
        }
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (!_methodServiceFilterOption.CurrentValue.IsEnabled)
        {
            await context.Invoke();
            return;
        }

        if (ShouldSkip(context))
        {
            await context.Invoke();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await context.Invoke();
            await Track(context, stopwatch, false);
        }
        catch (Exception)
        {
            await Track(context, stopwatch, true);
            throw;
        }
    }

    private bool ShouldSkip(IIncomingGrainCallContext context)
    {
        var grainMethod = context.InterfaceMethod;
        return grainMethod == null ||
               _methodServiceFilterOption.CurrentValue.SkippedMethods.Contains(_methodFormatter(context));
    }

    private async Task Track(IIncomingGrainCallContext context, Stopwatch stopwatch, bool isException)
    {
        if (_n9EClientFactory == null)
        {
            return;
        }

        try
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var grainMethodName = _methodFormatter(context);
            var indexOf = grainMethodName.LastIndexOf(".", StringComparison.Ordinal);
            IDictionary<string, string>? properties = new Dictionary<string, string>()
            {
                {
                    N9EClientConstant.LabelInterface,
                    indexOf == -1 ? N9EClientConstant.Unknown : grainMethodName[..indexOf]
                },
                { N9EClientConstant.LabelMethod, grainMethodName[(indexOf + 1)..] },
                { N9EClientConstant.LabelSuccess, (!isException).ToString() }
            };
            await _n9EClientFactory.TrackAsync(client =>
            {
                client.TrackMetric(N9EClientConstant.MetricOrleansMethodsService, elapsedMs, properties);
            });
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "error recording results for grain");
        }
    }
}