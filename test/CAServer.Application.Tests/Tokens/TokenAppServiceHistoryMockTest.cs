using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.TokenPrice;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Core.Clusters;
using Moq;
using NSubstitute;
using Orleans;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;

namespace CAServer.Tokens;

public partial class TokenAppServiceHistoryTest
{
    private ITokenPriceProvider GetMockTokenPriceProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenPriceProvider>();
        mockTokenPriceProvider.Setup(o => o.GetPriceAsync(It.IsAny<string>()))
            .ReturnsAsync((string dto) => dto == Symbol ? 1000 : 100);

        return mockTokenPriceProvider.Object;
    }

    private static IOptions<ContractAddressOptions> GetMockContractAddressOptions()
    {
        var tokenClaimContractAddress = new TokenClaimAddress
        {
            ContractName = "test",
            MainChainAddress = "test",
            SideChainAddress = "test"
        };
        var contractAddressOptions = new ContractAddressOptions
        {
            TokenClaimAddress = tokenClaimContractAddress
        };

        return new OptionsWrapper<ContractAddressOptions>(contractAddressOptions);
    }

    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHistoryHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }

    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }


    private IDistributedCache<List<string>> GetMockSymbolCache()
    {
        var mockCache = new Mock<IDistributedCache<List<string>>>();

        mockCache.Setup(t => t.GetAsync(It.IsAny<string>(), null, false, default))
            .Returns((string key) => new List<string>() { "ELF" });
        return mockCache.Object;
    }
}

public class DelegatingHistoryHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHistoryHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }

    public DelegatingHistoryHandlerStub(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}