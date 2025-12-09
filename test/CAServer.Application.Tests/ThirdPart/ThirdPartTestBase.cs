using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Transak;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace CAServer.ThirdPart;

public class ThirdPartTestBase : CAServerApplicationTestBase
{
    internal readonly string PendingTxId = HashHelper.ComputeFrom("PENDING").ToHex();
    internal readonly string MinedTxId = HashHelper.ComputeFrom("MINED").ToHex();


    public ThirdPartTestBase(ITestOutputHelper output) : base(output)
    {
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockITokenProvider());
        services.AddSingleton(MockHttpFactory());
        MockHttpByPath(TransakApi.RefreshAccessToken.Method, TransakApi.RefreshAccessToken.Path,
            new TransakMetaResponse<object, TransakAccessToken>
            {
                Data = new TransakAccessToken
                {
                    AccessToken = "TransakAccessTokenMockData",
                    ExpiresAt = DateTime.UtcNow.AddHours(2).ToUtcSeconds()
                }
            });
        MockHttpByPath(TransakApi.UpdateWebhook.Method, TransakApi.UpdateWebhook.Path, "success");
        services.AddSingleton(TokenAppServiceTest.GetMockCoinGeckoOptions());
        services.AddSingleton(TokenAppServiceTest.GetMockTokenPriceWorkerOption());
        services.AddSingleton(TokenAppServiceTest.GetMockSignatureServerOptions());
        services.AddSingleton(TokenAppServiceTest.GetMockRequestLimitProvider());
    }


    protected static IOptionsMonitor<ThirdPartOptions> MockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "ramp",
                BaseUrl = "http://localhost:9200/book/_search",
                NftAppId = "test",
                NftBaseUrl = "http://localhost:9200/book/_search",
                UpdateSellOrderUri = "/webhooks/off/merchant",
                FiatListUri = "/merchant/fiat/list",
                CryptoListUri = "/merchant/crypto/list",
                OrderQuoteUri = "/merchant/order/quote",
                GetTokenUri = "/merchant/getToken",
                MerchantQueryTradeUri = "/merchant/query/trade",
                TimestampExpireSeconds = int.MaxValue
            },
            Transak = new TransakOptions
            {
                AppId = "transakAppId",
                BaseUrl = "http://127.0.0.1:9200"
            },
            OrderExportAuth = new OrderExportAuth
            {
                Key = "test"
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 100,
                DelaySeconds = 1,
                HandleUnCompletedOrderMinuteAgo = 0,
                NftUnCompletedMerchantCallbackMinuteAgo = 0,
                HandleUnCompletedSettlementTransferSecondsAgo = 0,
                TransactionWaitTimeoutSeconds = 2,
                TransactionWaitDelaySeconds = 1,
            },
            TreasuryOptions = new TreasuryOptions
            {
                SettlementPublicKey = new Dictionary<string, string>
                {
                    ["Alchemy_USDT"] = "042dc50fd7d211f16bf4ad870f7790d4f9d98170f3712038c45830947f7d96c691ef2d1ab4880eeeeafb63ab77571be6cbe6bed89d5f89844b0fb095a7015713c8" 
                }
            },
            Merchant = new MerchantOptions
            {
                Merchants = new Dictionary<string, MerchantItem>
                {
                    ["symbolMarket"] = new MerchantItem
                    {
                        PublicKey =
                            "042dc50fd7d211f16bf4ad870f7790d4f9d98170f3712038c45830947f7d96c691ef2d1ab4880eeeeafb63ab77571be6cbe6bed89d5f89844b0fb095a7015713c8",
                    }
                }
            }
        };

        var optionMock = new Mock<IOptionsMonitor<ThirdPartOptions>>();
        optionMock.Setup(o => o.CurrentValue).Returns(thirdPartOptions);
        return optionMock.Object;
    }
    
    
    protected static ISecretProvider MockSecretProvider()
    {
        var rampSecret = "rampTest";
        var nftSecret = "testTest";
        var option = MockThirdPartOptions();
        var mock = new Mock<ISecretProvider>();
        mock.Setup(ser => ser.GetSecretWithCacheAsync(option.CurrentValue.Transak.AppId)).Returns(Task.FromResult("transakAppSecret"));
        
        mock.Setup(ser => ser.GetAlchemyShaSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.GenerateAlchemyApiSign(appid + rampSecret + source)));
        mock.Setup(ser => ser.GetAlchemyAesSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.AesEncrypt(source, rampSecret)));
        mock.Setup(ser => ser.GetAlchemyHmacSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.HmacSign(source, rampSecret)));
        
        mock.Setup(ser => ser.GetAlchemyShaSignAsync(option.CurrentValue.Alchemy.NftAppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.GenerateAlchemyApiSign(appid + nftSecret + source)));
        mock.Setup(ser => ser.GetAlchemyAesSignAsync(option.CurrentValue.Alchemy.NftAppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.AesEncrypt(source, nftSecret)));
        mock.Setup(ser => ser.GetAlchemyHmacSignAsync(option.CurrentValue.Alchemy.NftAppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.HmacSign(source, nftSecret)));
        return mock.Object;
    }


    protected static IOptionsMonitor<RampOptions> MockRampOptions()
    {
        var rampOption = new RampOptions
        {
            Providers = new Dictionary<string, ThirdPartProvider>
            {
                ["Alchemy"] = new()
                {
                    AppId = "test",
                    BaseUrl = "http://127.0.0.1:9200",
                    Name = "AlchemyPay",
                    Logo = "http://127.0.0.1:9200/logo.png",
                    WebhookUrl = "http://127.0.0.1:9200",
                    CountryIconUrl = "https://static.alchemypay.org/alchemypay/flag/{ISO}.png",
                    PaymentTags = new List<string> { "ApplePay", "GooglePay" },
                    NetworkMapping = new Dictionary<string, string>
                    {
                        ["AELF"] = "ELF"
                    },
                    SymbolMapping = new Dictionary<string, string>
                    {
                        ["USDT"] = "USDT-aelf"  
                    },
                    Coverage = new ProviderCoverage
                    {
                        OffRamp = true,
                        OnRamp = true
                    }
                },
                ["Transak"] = new()
                {
                    AppId = "test",
                    BaseUrl = "http://127.0.0.1:9200",
                    Name = "TransakPay",
                    Logo = "http://127.0.0.1:9200/logo.png",
                    WebhookUrl = "http://127.0.0.1:9200",
                    CountryIconUrl = "https://static.alchemypay.org/alchemypay/flag/{ISO}.png",
                    PaymentTags = new List<string> { "ApplePay", "GooglePay" },
                    Coverage = new ProviderCoverage
                    {
                        OffRamp = true,
                        OnRamp = true
                    },
                    NetworkMapping = new Dictionary<string, string>
                    {
                        ["AELF"] = "aelf"
                    }
                }
            },
            PortkeyIdWhiteList = new List<string>(),
            DefaultCurrency = new DefaultCurrencyOption(),
            CryptoList = new List<CryptoItem>
            {
                new()
                {
                    Symbol = "ELF",
                    Icon = "http://127.0.0.1:9200/elf.png",
                    Decimals = "8",
                    Network = "AELF",
                    Address = "0x00000000"
                },
                new()
                {
                    Symbol = "USDT",
                    Icon = "http://127.0.0.1:9200/usdt.png",
                    Decimals = "8",
                    Network = "AELF",
                    Address = "0x00000000"
                }
            },
            CoverageExpressions = new Dictionary<string, CoverageExpression>
            {
                ["Alchemy"] = new()
                {
                    OnRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        // "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    },
                    OffRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        // "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    }
                },
                ["Transak"] = new()
                {
                    OnRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        // "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    },
                    OffRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        // "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    }
                }
            }
        };


        var optionMock = new Mock<IOptionsMonitor<RampOptions>>();
        optionMock.Setup(o => o.CurrentValue).Returns(rampOption);
        return optionMock.Object;
    }

    protected IBus MockMassTransitIBus()
    {
        var mockContractProvider = new Mock<IBus>();
        return mockContractProvider.Object;
    }

    protected IContractProvider MockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider
            .Setup(p =>
                p.SendRawTransactionAsync("AELF", It.IsAny<string>()))
            .ReturnsAsync(new SendTransactionOutput { TransactionId = PendingTxId });

        mockContractProvider
            .Setup(p => p.GetTransactionResultAsync(It.IsAny<string>(), PendingTxId))
            .ReturnsAsync(new TransactionResultDto
            {
                TransactionId = PendingTxId,
                Status = "PENDING",
                Transaction = new TransactionDto()
            });

        mockContractProvider
            .Setup(p => p.GetTransactionResultAsync(It.IsAny<string>(), MinedTxId))
            .ReturnsAsync(new TransactionResultDto
            {
                TransactionId = MinedTxId,
                Status = "MINED",
                Transaction = new TransactionDto(),
                BlockNumber = 10,
            });

        mockContractProvider
            .Setup(p => p.GenerateTransferTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Tuple<string, Transaction>(PendingTxId, new Transaction()
            {
                From = Address.FromBase58("izyQqrVvraoDC69HvUX8gEAgNrK3hWq9qhUKh5vh4MfzNjfc6"),
                To = Address.FromBase58("izyQqrVvraoDC69HvUX8gEAgNrK3hWq9qhUKh5vh4MfzNjfc6")
            }));

        mockContractProvider
            .Setup(p => p.GetChainStatusAsync(It.IsAny<string>()))
            .ReturnsAsync(new ChainStatusDto
            {
                BestChainHeight = 100,
                LongestChainHeight = 100,
                LastIrreversibleBlockHeight = 100,
                GenesisBlockHash = HashHelper.ComputeFrom("").ToHex()
            });
        return mockContractProvider.Object;
    }

    protected IGraphQLProvider MockGraphQlProvider()
    {
        var mockGraphQlClient = new Mock<IGraphQLProvider>();
        mockGraphQlClient
            .Setup(p => p.GetIndexBlockHeightAsync("AELF"))
            .ReturnsAsync(100);

        return mockGraphQlClient.Object;
    }

    protected ITokenProvider MockTokenPrivider()
    {
        var tokenProvider = new Mock<ITokenProvider>();
        tokenProvider
            .Setup(p => p.GetTokenInfosAsync(It.IsAny<string>(), "ELF", It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokens
            {
                TokenInfo = new List<IndexerToken>
                {
                    new()
                    {
                        Symbol = "ELF",
                        Decimals = 8,
                    }
                }
            });

        return tokenProvider.Object;
    }

    protected IOptionsMonitor<ChainOptions> MockChainOptions()
    {
        var chainOptions = new ChainOptions()
        {
            ChainInfos = new Dictionary<string, Options.ChainInfo>()
            {
                [CommonConstant.MainChainId] = new()
                {
                    ChainId = CommonConstant.MainChainId,
                }   
            }
        };
        
        var mock = new Mock<IOptionsMonitor<ChainOptions>>();
        mock.Setup(p => p.CurrentValue).Returns(chainOptions);
        return mock.Object;
    }
    
        
    private ITokenProvider GetMockITokenProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenProvider>();
        mockTokenPriceProvider
            .Setup(o => o.GetTokenInfoAsync(It.IsAny<string>(),"ELF"))
            .ReturnsAsync(new IndexerToken()
                {
                    Id = "AELF",
                    Decimals = 8,
                });
        mockTokenPriceProvider
            .Setup(o => o.GetTokenInfoAsync(It.IsAny<string>(),"USDT"))
            .ReturnsAsync(new IndexerToken()
                {
                    Id = "USDT",
                    Decimals = 6
                });

        return mockTokenPriceProvider.Object;
    }

    protected IOptionsMonitor<ExchangeOptions> MockExchangeOptions()
    {
        var options = new ExchangeOptions
        {
            Binance = new BinanceOptions
            {
                BaseUrl = "http://127.0.0.1:9200"
            },
            Okx = new OkxOptions
            {
                BaseUrl = "http://127.0.0.1:9200"
            }
        };
        
        
        var mock = new Mock<IOptionsMonitor<ExchangeOptions>>();
        mock.Setup(p => p.CurrentValue).Returns(options);
        return mock.Object;
    }
}