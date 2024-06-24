using System.Threading.Tasks;
using CAServer.Growth.Dtos;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        services.AddSingleton(MockIActivityProvider());
    }
    
    [Fact]
    public async Task ReferralRecordList_Test()
    {
        var param = new ReferralRecordRequestDto
        {
            Skip = 0,
            Limit = 10
        };
        var result = await _statisticAppService.GetReferralRecordList(param);
        result.HasNextPage.ShouldBe(false);
    
    }

    [Fact]
    public async Task GetReferralTotalCountAsync_Test()
    {

        var result = await _statisticAppService.GetReferralTotalCountAsync(new ReferralRecordRequestDto());
        result.ShouldBe(1);
    }

    [Fact]
    public async Task CalculateReferralRankAsync_Test()
    {
        await _statisticAppService.CalculateReferralRankAsync();
    }

    [Fact]
    public async Task InitReferralRankAsync_Test()
    {
        await _statisticAppService.InitReferralRankAsync();
    }
    
  



}