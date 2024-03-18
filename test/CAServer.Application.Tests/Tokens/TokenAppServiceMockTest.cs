using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.Tokens.TokenPrice;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;

namespace CAServer.Tokens;

public partial class TokenAppServiceTest
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
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }

    private ITokenCacheProvider GetMockITokenCacheProvider()
    {
        var mockTokenCacheProvider = new Mock<ITokenCacheProvider>();
        mockTokenCacheProvider.Setup(o =>
            o.GetTokenInfoAsync(It.IsAny<string>(), It.IsAny<string>(), TokenType.Token))
            .ReturnsAsync((string chainId, string symbol) =>
            {
                if (symbol == "AXX")
                {
                    return new GetTokenInfoDto
                    {
                        Symbol = "AXX",
                        Decimals = 8
                    };
                }

                return new GetTokenInfoDto();
            });
        return mockTokenCacheProvider.Object;
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
                    },
                    IsDefault = true
                },
                new UserTokenIndex()
                {
                    Id = Guid.NewGuid(),
                    Token = new Entities.Es.Token()
                    {
                        Symbol = "ELF",
                        ChainId = "AELF",
                        Decimals = 8
                    },
                    IsDefault = true,
                    IsDisplay = true
                },
                new UserTokenIndex()
                {
                    Id = Guid.NewGuid(),
                    Token = new Entities.Es.Token()
                    {
                        Symbol = "VCTE",
                        ChainId = "AELF",
                        Decimals = 8
                    },
                    IsDisplay = true
                }
            });

        mockTokenPriceProvider.Setup(o =>
                o.GetTokenInfosAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 0, 200))
            .ReturnsAsync((string chainId, string symbol, string symbolKeyword, int skipCount, int maxResultCount) =>
            {
                return new IndexerTokens()
                {
                    TokenInfo = new List<IndexerToken>()
                    {
                        new IndexerToken()
                        {
                            Id = "AELF-CPU",
                            Symbol = "CPU",
                            ChainId = "AELF",
                            Decimals = 8,
                            BlockHash = string.Empty,
                            BlockHeight = 0,
                            Type = string.Empty,
                            TokenContractAddress = string.Empty,
                            TokenName = "CPU",
                            TotalSupply = 100000,
                            Issuer = string.Empty,
                            IsBurnable = false,
                            IssueChainId = 1264323
                        },
                        new IndexerToken()
                        {
                            Id = "tDVV-CPU",
                            Symbol = "CPU",
                            ChainId = "tDVV",
                            Decimals = 8,
                            BlockHash = string.Empty,
                            BlockHeight = 0,
                            Type = string.Empty,
                            TokenContractAddress = string.Empty,
                            TokenName = "CPU",
                            TotalSupply = 100000,
                            Issuer = string.Empty,
                            IsBurnable = false,
                            IssueChainId = 1264323
                        }
                    }
                };
            });

        mockTokenPriceProvider.Setup(o =>
                o.GetUserTokenInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid userId, string chainId, string symbol) =>
            {
                if (symbol == "VOTE")
                {
                    return null;
                }

                if (symbol == "AXX")
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