using System.Runtime.CompilerServices;
using Serilog;

namespace Orleans.TelemetryConsumers.Nightingale;

public class NightingaleClient
{
    internal static ILogger Logger { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void RecordMetric(string name, float value)
    {
        //Logger.Warning(string.Format("Nightingale.RecordMetric({0},{1})", (object)name, (object)value));
        Logger.Warning(string.Format("{0} {1}", (object)name, (object)value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void RecordResponseTimeMetric(string name, long millis)
    {
        // Logger.Warning(string.Format("Nightingale.RecordResponseTimeMetric({0},{1})", (object)name, (object)millis));
        Logger.Warning(string.Format("{0},{1}", (object)name, (object)millis));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void IncrementCounter(string name)
    {
        // Logger.Warning(string.Format("Nightingale.IncrementCounter({0})", (object)name));
        Logger.Warning(string.Format("{0}", (object)name));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void NoticeError(Exception exception, IDictionary<string, string> parameters)
    {
        // Logger.Warning(string.Format("Nightingale.NoticeError({0},{1})", (object)exception, (object)parameters));
        Logger.Warning(string.Format("{0},{1}", (object)exception, (object)parameters));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void NoticeError(Exception exception)
    {
        Logger.Warning(string.Format("Nightingale.NoticeError({0})", (object)exception));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void NoticeError(string message, IDictionary<string, string> parameters)
    {
        Logger.Warning(string.Format("Nightingale.NoticeError({0},{1})", (object)message, (object)parameters));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void AddCustomParameter(string key, IConvertible value)
    {
        // Logger.Warning(string.Format("Nightingale.AddCustomParameter({0},{1})", (object)key, (object)value));
        Logger.Warning(string.Format("{0},{1}", (object)key, (object)value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void AddCustomParameter(string key, string value)
    {
        // Logger.Warning(string.Format("Nightingale.AddCustomParameter({0},{1})", (object)key, (object)value));
        Logger.Warning(string.Format("{0},{1}", (object)key, (object)value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SetTransactionName(string category, string name)
    {
        Logger.Warning(string.Format("Nightingale.SetTransactionName({0},{1})", (object)category, (object)name));
    }

    public static void SetTransactionUri(Uri uri) =>
        Logger.Warning(string.Format("Nightingale.SetUri({0})", (object)uri));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void IgnoreTransaction() => Logger.Warning("Nightingale.IgnoreTransaction()");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void IgnoreApdex() => Logger.Warning("Nightingale.IgnoreApdex()");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static string GetBrowserTimingHeader()
    {
        Logger.Warning("Nightingale.GetBrowserTimingHeader()");
        return "<!-- New Relic Header -->";
    }

    [Obsolete]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static string GetBrowserTimingFooter() => "<!-- New Relic Footer is Obsolete -->";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void DisableBrowserMonitoring(bool overrideManual = false) =>
        Logger.Warning("Nightingale.DisableBrowserMonitoring()");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SetUserParameters(string userName, string accountName, string productName) =>
        Logger.Warning("Nightingale.SetUserParameters(String userName, String accountName, String productName)");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void RecordCustomEvent(
        string eventType,
        IEnumerable<KeyValuePair<string, object>> attributes)
    {
        // Logger.Warning(
        //     string.Format("Nightingale.RecordCustomEvent({0},{1})", (object)eventType, (object)attributes));
        Logger.Warning(
        string.Format("{0},{1}", (object)eventType, (object)attributes));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void StartAgent() => Logger.Warning("#Nightingale.StartAgent()");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SetApplicationName(
        string applicationName,
        string applicationName2 = null,
        string applicationName3 = null)
    {
        Logger.Warning(
            "Nightingale.SetApplicationName(String applicationName, String applicationName2 = null, String applicationName3 = null)");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static IEnumerable<KeyValuePair<string, string>> GetRequestMetadata()
    {
        Logger.Warning("Nightingale.GetRequestMetadata()");
        return (IEnumerable<KeyValuePair<string, string>>)null;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static IEnumerable<KeyValuePair<string, string>> GetResponseMetadata()
    {
        Logger.Warning("Nightingale.GetResponseMetadata()");
        return (IEnumerable<KeyValuePair<string, string>>)null;
    }
}