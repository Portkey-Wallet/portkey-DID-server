
using Microsoft.Extensions.Hosting;

namespace CAServer.Commons;

public static class EnvHelper
{
    private static string _hostingEnvironment;

    public static void Init(string evn)
    {
        _hostingEnvironment ??= evn;
    }
    
    public static bool IsDevelopment()
    {
        return _hostingEnvironment == null ||  _hostingEnvironment == Environments.Development;
    }
    
    public static bool IsStaging()
    {
        return _hostingEnvironment == Environments.Staging;
    }

    public static bool IsProduction()
    {
        return !IsDevelopment() && !IsStaging();
    }
    
    public static bool NotProduction()
    {
        return IsDevelopment() || IsStaging();
    }
    
}