using System.Threading.Tasks;
using CAServer.Growth.Dtos;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Growth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class GrowthServiceTest : CAServerApplicationTestBase
{
    private readonly IGrowthStatisticAppService _statisticAppService;
    private ICurrentUser _currentUser;

    public GrowthServiceTest()
    {
        _statisticAppService = GetRequiredService<IGrowthStatisticAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockSecretProvider());
        services.AddSingleton(MockUserAssetsProvider());
        services.AddSingleton(MockGrowthProvider());
    }
    
    // [Fact]
    // public async Task VerifierGoogleReCaptcha_Test()
    // {
    //     var param = new ReferralRecordRequestDto
    //     {
    //         CaHash = "",
    //         Skip = 0,
    //         Limit = 10
    //     };
    //     var result = await _statisticAppService.GetReferralRecordList(param);
    //
    // }
    //
    
    
}