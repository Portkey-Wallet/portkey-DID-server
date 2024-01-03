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
    private const string VerifierServerId = "50986afa3095f66bd590d6ab26218cc2ed2ef4b1f6e7cdab5b3cbb2cd8a540f8";

    public GetVerifierServerProviderTest()
    {
        _getVerifierServerProvider = GetRequiredService<IGetVerifierServerProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetAdaptableVariableOptions());
        services.AddSingleton(GetMockContractProvider());
    }

    [Fact]
    public async Task GetVerifierServerProvider_null_Test()
    {
        var result = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(DefaultVerifierId,DefaultChainId);
        result.ShouldBe(null);
    }
    
    [Fact]
    public async Task GetVerifierServerProvider_null_Test1()
    {
        var result = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(DefaultVerifierId,"ABCD");
        result.ShouldBe(null);
    }
    
    [Fact]
    public async Task GetVerifierServerProvider_null_Test2()
    {
        var result = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(DefaultVerifierId,DefaultChainId);
        result.ShouldBe(null);
    }

    
    
    [Fact]
    public async Task GetVerifierServerProvider_Test()
    {
        var result = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(VerifierServerId,DefaultChainId);
        result.ShouldBe("http://127.0.0.1:1122");
    }
    
}