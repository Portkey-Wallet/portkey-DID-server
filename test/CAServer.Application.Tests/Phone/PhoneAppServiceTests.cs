using CAServer.IpInfo;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Phone;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class PhoneInfoServiceTests : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    private readonly IPhoneAppService _phoneAppService;
    private readonly IIpInfoClient _ipInfoClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PhoneInfoServiceTests()
    {
        _phoneAppService = GetService<IPhoneAppService>();
        _httpContextAccessor = GetService<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext = new DefaultHttpContext();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetPhoneInfoOptions());
        services.AddSingleton(IpInfoClientTest.MockIpInfoHttpClient());
    }

    [Fact]
    public async void PhoneInfoSuccessTest()
    {
        
        
        var inputImagePath = "/Users/wangyue/Desktop/SEED-100000001.svg";
        var  outputImagePath = "/Users/wangyue/Desktop/100000001.png";

        try
        {
            using var image = new MagickImage(inputImagePath);
            image.Format = MagickFormat.Png;

            image.Write(outputImagePath);
                
            //_logger.LogDebug("image format convert success！");
        }
        catch (MagickException ex)
        {
            //_logger.LogError($"image format failed : {ex.Message}");
        }
        
        
        
        _httpContextAccessor.HttpContext.Request.Headers.Add("X-Forwarded-For", "0.0.0.0");
        var res = await _phoneAppService.GetPhoneInfoAsync();
        
        // data list test
        res.Data.Count.ShouldBe(2);
        res.Data[0]["country"].ShouldBe("Singapore");
        res.Data[0]["code"].ShouldBe("65");
        res.Data[0]["iso"].ShouldBe("SG");

        // locate not match
        res.LocateData["country"].ShouldBe("Singapore");
        res.LocateData["code"].ShouldBe("65");
        res.LocateData["iso"].ShouldBe("SG");

        
        // locate match
        _httpContextAccessor.HttpContext.Request.Headers.Remove("X-Forwarded-For");
        _httpContextAccessor.HttpContext.Request.Headers.Add("X-Forwarded-For", "20.230.34.112");
        res = await _phoneAppService.GetPhoneInfoAsync();
        res.LocateData["country"].ShouldBe("United States");
        res.LocateData["code"].ShouldBe("1");
        res.LocateData["iso"].ShouldBe("US");
    }
}