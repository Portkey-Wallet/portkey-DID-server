using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Core.Clusters;
using Moq;
using NSubstitute;
using Org.BouncyCastle.Bcpg;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public partial class TokenAppServiceTest
{
    private ITokenPriceGrain GetMockTokenPriceGrain()
    {
        var mockITokenPriceGrainClient = new Mock<ITokenPriceGrain>();
        mockITokenPriceGrainClient.Setup(o => o.GetCurrentPriceAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new GrainResultDto<TokenPriceGrainDto>()
                {
                    Success = true,
                    Data = new TokenPriceGrainDto()
                    {
                        Symbol = Symbol,
                        PriceInUsd = 1000,
                    }
                }
            );

        return mockITokenPriceGrainClient.Object;
    }

    private ITokenPriceSnapshotGrain GetMockTokenPriceSnapshotGrain()
    {
        var mockITokenPriceGrainClient = new Mock<ITokenPriceSnapshotGrain>();
        mockITokenPriceGrainClient.Setup(o => o.GetHistoryPriceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(
                new GrainResultDto<TokenPriceGrainDto>()
                {
                    Success = true,
                    Data = new TokenPriceGrainDto()
                    {
                        Symbol = Symbol,
                        PriceInUsd = 1000,
                    }
                }
            );
        return mockITokenPriceGrainClient.Object;
    }

    private ITokenPriceProvider GetMockTokenPriceProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenPriceProvider>();
        mockTokenPriceProvider.Setup(o => o.GetPriceAsync(It.IsAny<string>()))
            .ReturnsAsync((string dto) => dto == Symbol ? 1000 : 100);

        return mockTokenPriceProvider.Object;
    }

    private IOptions<TokenPriceExpirationTimeOptions> GetMockTokenPriceExpirationTimeOptions()
    {
        return new OptionsWrapper<TokenPriceExpirationTimeOptions>(new TokenPriceExpirationTimeOptions()
        {
            Time = 100
        });
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
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }

    private IClusterClient GetMockClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IGrainWithStringKey>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string primaryKey, string namePrefix) => { return GetMockTokenPriceGrain(); });
        return mockClusterClient.Object;
    }

    private IClusterClient GetMockTokenPriceSnapshotClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IGrainWithStringKey>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string primaryKey, string namePrefix) => { return GetMockTokenPriceSnapshotGrain(); });
        return mockClusterClient.Object;
    }

    private ITokenProvider GetMockITokenProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenProvider>();


        mockTokenPriceProvider.Setup(o =>
                o.GetUserTokenInfoListAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UserTokenIndex>
            {
                new UserTokenIndex()
                {
                    Id = Guid.NewGuid(),
                    Token = new Entities.Es.Token()
                    {
                        Symbol = "CPU",
                        ChainId = "AELF",
                        Decimals = 8
                    }
                }
            });

        mockTokenPriceProvider.Setup(o =>
                o.GetTokenInfosAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default, default))
            .ReturnsAsync(new IndexerTokens()
            {
                TokenInfo = new List<IndexerToken>()
                {
                    new IndexerToken()
                    {
                        Id = "AELF",
                        Symbol = "CPU",
                        ChainId = "AELF",
                        Decimals = 8
                    }
                }
            });

        mockTokenPriceProvider.Setup(o =>
                o.GetUserTokenInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid userId, string chainId, string symbol) =>
            {
                if (symbol == "VOTE")
                {
                    return null;
                }

                return new UserTokenIndex()
                {
                    IsDisplay = false,
                    IsDefault = false,
                    Token = new CAServer.Entities.Es.Token()
                    {
                        Symbol = "CPU",
                        ChainId = "AELF",
                        Decimals = 8
                    }
                };
            });

        return mockTokenPriceProvider.Object;
    }

    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }

    public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}