using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CAServer.Commons;

public static class EnvHelper
{
    private static IWebHostEnvironment _hostingEnvironment;

    public static void Init(IWebHostEnvironment hostingEnvironment)
    {
        if (_hostingEnvironment == null) _hostingEnvironment = hostingEnvironment;
    }
    
    public static bool IsDevelopment()
    {
        return _hostingEnvironment == null || _hostingEnvironment.IsDevelopment();
    }
    
}