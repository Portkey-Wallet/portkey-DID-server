using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.IpInfo;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class IpInfoServiceTest : CAServerApplicationTestBase
{
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly IHttpContextAccessor _httpContextAccessor;
                                                                                        
    public IpInfoServiceTest()
    {
        _ipInfoAppService = GetService<IIpInfoAppService>();
        _httpContextAccessor = GetService<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Request.Headers.Add("X-Forwarded-For", "43.33.44.43");
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetIpInfoClient());
    }

    [Fact]
    public async Task TestGetIpInfo()
    {
        var ipInfoDto = new IpInfoDto();
        var ipInfo = await _ipInfoAppService.GetIpInfoAsync();
        ipInfo.ShouldNotBeNull();
        ipInfo.Country.ShouldBe("Singapore");

        var ipInfoFromCache = await _ipInfoAppService.GetIpInfoAsync();
        ipInfoFromCache.ShouldNotBeNull();
        ipInfo.Country.ShouldBe("Singapore");
    }
}