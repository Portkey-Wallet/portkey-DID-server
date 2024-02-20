using System.Threading.Tasks;
using CAServer.ClaimToken.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.ClaimToken;

[Collection(CAServerTestConsts.CollectionDefinitionName)]

public partial class ClaimTokenTests : CAServerApplicationTestBase
{

    private readonly IClaimTokenAppService _claimTokenAppService;

    public ClaimTokenTests()
    {
        _claimTokenAppService = GetRequiredService<IClaimTokenAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
     {
         base.AfterAddApplication(services);
        services.AddSingleton(GetClaimTokenInfoOptions());
        services.AddSingleton(GetMockCacheProvider());
        services.AddSingleton(GetClaimTokenWhiteListAddressesOptions());
        services.AddSingleton(GetMockContractProvider());
     }
    

    [Fact]
    public async Task GetClaimTokenAsyncTest()
    {
        var requestDto = new ClaimTokenRequestDto()
        {
            Symbol = "MockSymbol",
            Amount = "100",
            Address = "MockAddress"
        };
        var result = await _claimTokenAppService.GetClaimTokenAsync(requestDto);
        result.ShouldNotBe(null);
    }

}