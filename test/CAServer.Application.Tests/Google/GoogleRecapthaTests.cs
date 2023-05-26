using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.Google;

public partial class GoogleRecapthaTests : CAServerApplicationTestBase
{
    private readonly IGoogleAppService _googleAppService;

    public GoogleRecapthaTests()
    {
        _googleAppService = GetRequiredService<IGoogleAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(GetGoogleRecaptchaOptions());
        services.AddSingleton(GetMockCacheProvider());
        base.AfterAddApplication(services);
    }


    [Fact]
    public async Task VerifierGoogleReCaptcha_Test()
    {
        var token = "1234567890";
        var result = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(token);
        result.ShouldBeTrue();
    }
    
    [Fact]
    public async Task VerifierGoogleReCaptcha_InvalidateToken_Test()
    {
        var token = "";
        var result = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(token);
        result.ShouldBeFalse();
    }

    // [Fact]
    // public async Task IsGoogleRecaptchaOpen_Test()
    // {
    //     var userIpAddress = "127.0.0.1";
    //     var result = await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress);
    //     result.ShouldBeFalse();
    // }
}