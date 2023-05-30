using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Guardian.Provider;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CAServer.Guardian;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class GuardianProviderTest : CAServerApplicationTestBase
{
    private readonly IGuardianProvider _guardianProvider;

    public GuardianProviderTest()
    {
        _guardianProvider = GetRequiredService<IGuardianProvider>();
    }

    protected override void BeforeAddApplication(IServiceCollection services)
    {
        var graphQlHelper = Substitute.For<IGraphQLHelper>();
        var graphQlClient = Substitute.For<IGraphQLClient>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);
    }

    [Fact]
    public async Task GetGuardiansTest()
    {
        try
        {
            await _guardianProvider.GetGuardiansAsync(string.Empty, string.Empty);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetHolderInfoFromContractTest()
    {
        try
        {
            await _guardianProvider.GetHolderInfoFromContractAsync("test", "f2393d8b29f3e19c46f3ad3b3e851689c7b6724f11e0851b7b287fbae6e7a4e7",
                new Grains.Grain.ApplicationHandler.ChainInfo());
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
    
    [Fact]
    public async Task GetHolderInfoFromContract_Param_Test()
    {
        try
        {
            await _guardianProvider.GetHolderInfoFromContractAsync("test", string.Empty,
                new Grains.Grain.ApplicationHandler.ChainInfo());
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}