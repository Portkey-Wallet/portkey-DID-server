using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Tokens.Provider;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace CAServer.Tokens;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class TokenProviderTest : CAServerApplicationTestBase
{
    private readonly ITokenProvider _tokenProvider;

    public TokenProviderTest()
    {
        _tokenProvider = GetRequiredService<ITokenProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        var graphQlHelper = Substitute.For<IGraphQLHelper>();
        var graphQlClient = Substitute.For<IGraphQLClient>();
        services.AddSingleton(graphQlClient);
        services.AddSingleton(graphQlHelper);
    }

    [Fact]
    public async Task GetTokenInfosAsyncTest()
    {
        try
        {
            await _tokenProvider.GetTokenInfosAsync("AELF", "ELF", "", 0, 100);
        }
        catch (Exception e)
        {
        }
    }

    [Fact]
    public async Task GetUserTokenInfoAsyncTest()
    {
        var result = await _tokenProvider.GetUserTokenInfoAsync(Guid.NewGuid(), "AELF", "ELF");
    }

    [Fact]
    public async Task GetUserTokenInfoListAsyncTest()
    {
        var result = await _tokenProvider.GetUserTokenInfoListAsync(Guid.NewGuid(), "ELF", "");
    }
}