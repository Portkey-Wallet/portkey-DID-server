using System.Threading.Tasks;
using CAServer.Signature.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace CAServer.Google;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class GoogleRecaptchaTests : CAServerApplicationTestBase
{
    private readonly IGoogleAppService _googleAppService;

    public GoogleRecaptchaTests()
    {
        _googleAppService = GetRequiredService<IGoogleAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(GetGoogleRecaptchaOptions());
        services.AddSingleton(GetMockCacheProvider());
        services.AddSingleton(MockSecretProvider());
    }


    [Fact]
    public async Task VerifierGoogleReCaptcha_Test()
    {
        var token = "1234567890";
        var result = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(token, PlatformType.IOS);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifierGoogleReCaptcha_InvalidateToken_Test()
    {
        var token = "";
        var result = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(token, PlatformType.IOS);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsGoogleRecaptchaOpen_Test()
    {
        var version = "v12345";
        var userIpAddress = "127.0.0.1";
        var result = await _googleAppService.IsGoogleRecaptchaOpenAsync(userIpAddress, OperationType.AddGuardian);
        result.ShouldBeTrue();
    }
}