using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.CoinGeckoApi;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.Tokens.TokenPrice;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Caching;

namespace CAServer.Tokens;

public partial class TokenAppServiceTest
{
    public static ITokenPriceProvider GetMockTokenPriceProvider()
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

    public static IOptionsMonitor<CoinGeckoOptions> GetMockCoinGeckoOptions()
    {
        var mock = new Mock<IOptionsMonitor<CoinGeckoOptions>>();
        mock.Setup(o => o.CurrentValue).Returns(() => new CoinGeckoOptions
        {
            BaseUrl = "",
            CoinIdMapping = new Dictionary<string, string> { { "ALEF", "aelf" } },
            Priority = 0,
            IsAvailable = true,
            Timeout = -1
        });
        return mock.Object;
    }
    
    public static IOptionsMonitor<TokenSpenderOptions> GetMockTokenSpenderOptions()
    {
        var mock = new Mock<IOptionsMonitor<TokenSpenderOptions>>();
        mock.Setup(o => o.CurrentValue).Returns(() => new TokenSpenderOptions
        {
            TokenSpenderList = new List<TokenSpender>()
            {
                new ()
                {
                        ChainId = "AELF",
                        ContractAddress = "XXXXX",
                        Name = "Dapp1",
                        Url = "https://sss.com",
                        Icon = "https://111.png",
                }
            }
        });
        return mock.Object;
    }

    public static IOptionsMonitor<TokenPriceWorkerOption> GetMockTokenPriceWorkerOption()
    {
        var mock = new Mock<IOptionsMonitor<TokenPriceWorkerOption>>();
        mock.Setup(o => o.CurrentValue).Returns(() => new TokenPriceWorkerOption
        {
            Symbols = new List<string>(){"ELF", "USDT"}
        });
        return mock.Object;
    }

    public static IOptionsMonitor<SignatureServerOptions> GetMockSignatureServerOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsMonitor<SignatureServerOptions>>();
        mockOptionsSnapshot.Setup(o => o.CurrentValue).Returns(
            new SignatureServerOptions
            {
                BaseUrl = "http://127.0.0.1:5577",
                AppId = "caserver",
                AppSecret = "12345678"
            });
        return mockOptionsSnapshot.Object;
    }

    public static IRequestLimitProvider GetMockRequestLimitProvider()
    {
        var mock = new Mock<IRequestLimitProvider>();
        mock.Setup(o => o.RecordRequestAsync()).Returns(() => { return Task.CompletedTask; });
        return mock.Object;
    }

    public static ISecretProvider GetMockSecretProvider()
    {
        var mock = new Mock<ISecretProvider>();
        mock.Setup(o => o.GetSecretWithCacheAsync(It.IsAny<string>())).ReturnsAsync("aaaa");
        return mock.Object;
    }

    public static IDistributedCache<string> GetMockDistributedCache()
    {
        var mock = new Mock<IDistributedCache<string>>();
        mock.Setup(o => o.GetAsync(It.IsAny<string>(), null, false, default(CancellationToken))).ReturnsAsync(
            (string key, bool? hideErrors, bool considerUow, CancellationToken token) =>
            {
                if (key.Contains(Symbol))
                {
                    return AelfPrice.ToString(CultureInfo.InvariantCulture);
                }
                else if (key.EndsWith(UsdtSymbol))
                {
                    return UsdtPrice.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    return "aa";
                }
            });
        mock.Setup(o => o.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mock.Object;
    }

    public static IHttpClientFactory GetMockHttpClientFactory()
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
            o.GetTokenInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TokenType>()))
            .ReturnsAsync((string chainId, string symbol, TokenType tokenType) =>
            {
                if (symbol == "AXX")
                {
                    return new GetTokenInfoDto
                    {
                        Symbol = "AXX",
                        Decimals = 8
                    };
                }
                if (symbol == "SGR-1")
                {
                    return new GetTokenInfoDto
                    {
                        Symbol = "SGR-1",
                        Decimals = 8
                    };
                }
                if (symbol == "ELF")
                {
                    return new GetTokenInfoDto
                    {
                        Symbol = "ELF",
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

        mockTokenPriceProvider.Setup(o =>
                o.GetTokenApprovedAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((string chainId, List<string> caAddresses, int skipCount, int maxResultCount) =>
            {
                if (caAddresses.Count == 0 || caAddresses[0].IsNullOrWhiteSpace())
                {
                    return new IndexerTokenApproved
                    {
                        CaHolderTokenApproved = new CAHolderTokenApproved()
                    };
                }
                

                return new IndexerTokenApproved()
                {
                    CaHolderTokenApproved = new CAHolderTokenApproved()
                    {
                        Data = new List<CAHolderTokenApprovedDto>()
                        {
                            new ()
                            {
                                ChainId = "AELF",
                                Spender = "XXXXX",
                                BatchApprovedAmount = 1,
                                Symbol = "SGR-*"
                            },
                            new ()
                            {
                                ChainId = "AELF",
                                Spender = "XXXXX",
                                BatchApprovedAmount = 1,
                                Symbol = "ELF"
                            }
                        },
                        TotalRecordCount = 2
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