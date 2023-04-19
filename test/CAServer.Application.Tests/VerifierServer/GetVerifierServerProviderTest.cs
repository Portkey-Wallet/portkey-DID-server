using System.Threading.Tasks;
using CAServer.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.VerifierServer;


[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class GetVerifierServerProviderTest : CAServerApplicationTestBase
{
    private readonly IGetVerifierServerProvider _getVerifierServerProvider;
    private const string DefaultChainId = "AELF";
    private const string DefaultVerifierId = "";

    public GetVerifierServerProviderTest()
    {
        _getVerifierServerProvider = GetRequiredService<IGetVerifierServerProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetAdaptableVariableOptions());
        services.AddSingleton(GetMockContractProvider());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task GetVerifierServerProvider_Test()
    {
        var result = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(DefaultVerifierId,DefaultChainId);
        result.ShouldBe(null);
    }
    
}