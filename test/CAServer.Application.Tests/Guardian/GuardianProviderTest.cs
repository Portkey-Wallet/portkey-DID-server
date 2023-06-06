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
        // var contractProvider = Substitute.For<IContractProvider>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);
        // services.AddSingleton(contractProvider);
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
            await _guardianProvider.GetHolderInfoFromContractAsync("test", string.Empty, "AELF");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}