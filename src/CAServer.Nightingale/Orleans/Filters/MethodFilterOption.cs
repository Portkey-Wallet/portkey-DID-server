namespace CAServer.Nightingale.Orleans.Filters;

public class MethodFilterOptions
{
    public bool IsEnabled { get; set; } = true;

    public ISet<string> SkippedMethods { get; set; } = new HashSet<string>();
}

public class MethodServiceFilterOptions : MethodFilterOptions
{
}

public class MethodCallFilterOptions : MethodFilterOptions
{
}

public static class MethodFilterContext
{
    public static IServiceProvider ServiceProvider { get; set; }
}