using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.UserAssets;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class TokenImageProviderTest: CAServerApplicationTestBase
{
    private ITokenImageProvider _tokenImageProvider;

    public TokenImageProviderTest()
    {
        _tokenImageProvider = GetRequiredService<ITokenImageProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetImageProcessProvider());
        services.AddSingleton(GetMockTokenInfoOptions());
        
    }
  

    [Fact]
    public async Task TokenImage_Test()
    {
        var result = await _tokenImageProvider.GetTokenImageAsync("NFT",0,0);
        result.ShouldNotBeNull();
        result.ShouldBe("MockImageUrl");

    }
    
    [Fact]
    public async Task TokenImages_Test()
    {
        var list = new List<string>
        {
            "NFT",
            "USDT"
        };
        var result = await _tokenImageProvider.GetTokenImagesAsync(list,0,0);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result["NFT"].ShouldBe("MockImageUrl");
        result["USDT"].ShouldBe("MockImageUrl");

    }



}