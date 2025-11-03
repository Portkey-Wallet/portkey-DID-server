using System;
using System.Collections.Generic;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Search;
using CAServer.Search.Dtos;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp.Application.Dtos;
using Token = CAServer.Entities.Es.Token;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

namespace CAServer.UserAssets;

public partial class UserAssetsTests
{
    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();

        mockUserAssetsProvider.Setup(m => m.GetUserChainIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(
            new IndexerChainIds
            {
                CaHolderManagerInfo = new List<UserChainInfo>
                {
                    new()
                    {
                        ChainId = "AELF"
                    },
                    new()
                    {
                        ChainId = "tDVV"
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
                m.GetUserTokenInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokenInfos
            {
                CaHolderTokenBalanceInfo = new CaHolderTokenBalanceInfo
                {
                    totalRecordCount = 2,
                    Data = new List<IndexerTokenInfo>
                    {
                        new()
                        {
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            ChainId = "AELF",
                            Balance = 1000,
                            TokenIds = new List<long> { 1 },
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "ELF",
                                Decimals = 8,
                                TokenContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                TokenName = "Token",
                                TotalSupply = 10000
                            }
                        },
                        new()
                        {
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            ChainId = "AELF",
                            Balance = 1000,
                            TokenIds = new List<long> { 1 },
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "CPU",
                                Decimals = 8,
                                TokenContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                TokenName = "Token",
                                TotalSupply = 10000
                            }
                        }
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
                m.GetUserNftCollectionInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new IndexerNftCollectionInfos
                {
                    CaHolderNFTCollectionBalanceInfo = new CaHolderNFTCollectionBalanceInfo
                    {
                        TotalRecordCount = 1,
                        Data = new List<IndexerNftCollectionInfo>
                        {
                            new()
                            {
                                CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                ChainId = "AELF",
                                TokenIds = new List<long> { 1 },
                                NftCollectionInfo = new NftCollectionInfo
                                {
                                    Symbol = "TEST-0",
                                    TokenName = "TEST",
                                    TotalSupply = 1000,
                                    Decimals = 1
                                }
                            }
                        }
                    }
                });

        mockUserAssetsProvider.Setup(m =>
                m.GetUserNftInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>()))
            .ReturnsAsync(
                new IndexerNftInfos
                {
                    CaHolderNFTBalanceInfo = new CaHolderNFTBalanceInfo
                    {
                        TotalRecordCount = 1,
                        Data = new List<IndexerNftInfo>
                        {
                            new()
                            {
                                CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                ChainId = "AELF",
                                Balance = 1000,
                                NftInfo = new NftInfo
                                {
                                    Symbol = "TEST-1",
                                    CollectionSymbol = "TEST-0",
                                    Decimals = 5,
                                    CollectionName = "TestCollection"
                                }
                            }
                        }
                    }
                });

        mockUserAssetsProvider.Setup(m => m.GetUserDefaultTokenSymbolAsync(It.IsAny<Guid>())).ReturnsAsync(
            new List<UserTokenIndex>
            {
                new()
                {
                    Id = Guid.Empty,
                    UserId = Guid.Empty,
                    IsDefault = true,
                    IsDisplay = true,
                    SortWeight = 100,
                    Token = new Token
                    {
                        Id = Guid.Empty,
                        Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                        ChainId = "AELF",
                        Symbol = "ELF",
                        Decimals = 8
                    }
                },
                new()
                {
                    Id = Guid.Empty,
                    UserId = Guid.Empty,
                    IsDefault = true,
                    IsDisplay = true,
                    SortWeight = 100,
                    Token = new Token
                    {
                        Id = Guid.Empty,
                        Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                        ChainId = "tDVV",
                        Symbol = "UF",
                        Decimals = 8
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
                m.GetRecentTransactionUsersAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new IndexerRecentTransactionUsers
                {
                    CaHolderTransactionAddressInfo = new CaHolderTransactionAddressInfo
                    {
                        TotalRecordCount = 1,
                        Data = new List<CAHolderTransactionAddress>
                        {
                            new()
                            {
                                Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                AddressChainId = "AELF",
                                CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                ChainId = "AELF",
                                TransactionTime = 1000
                            }
                        }
                    }
                });

        mockUserAssetsProvider.Setup(m =>
            m.SearchUserAssetsAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>())).ReturnsAsync(
            new IndexerSearchTokenNfts
            {
                CaHolderSearchTokenNFT = new CaHolderSearchTokenNFT
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerSearchTokenNft>
                    {
                        new()
                        {
                            ChainId = "AELF",
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            Balance = 1000,
                            TokenId = 1,
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "ELF",
                                Decimals = 8,
                                TokenContractAddress = "address",
                                TokenName = "tokenName",
                                TotalSupply = 1000
                            },
                            NftInfo = new NftInfo
                            {
                                Symbol = "ELF-NFT"
                            }
                        }
                    }
                }
            });

        mockUserAssetsProvider.Setup(m => m.GetUserNftInfoBySymbolAsync(It.IsAny<List<CAAddressInfo>>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new IndexerNftInfos
        {
            // CaHolderNFTBalanceInfo =
            // {
            //     TotalRecordCount = 10,
            //     Data = new List<IndexerNftInfo>()
            //     {
            //         new IndexerNftInfo()
            //         {
            //             Balance = 10,
            //             CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
            //             ChainId = "AELF",
            //             NftInfo = new NftInfo()
            //             {
            //                 Symbol = "SEED-01",
            //                 Decimals = 10,
            //                 ImageUrl = "MockImageUrl",
            //                 CollectionSymbol = "MockSymbol",
            //                 CollectionName = "Seed-02",
            //                 TokenName = "MockToken",
            //                 TotalSupply = 10000,
            //                 Supply = 1,
            //                 TokenContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
            //                 InscriptionName = "SGR-01",
            //                 Lim = "Lim",
            //                 Expires = "10",
            //                 SeedOwnedSymbol = "Seed-03",
            //                 Generation = "MockGeneration",
            //                 Traits =
            //                     @"[{""traitType"":""background"",""value"":""red""},{""traitType"":""color"",""value"":""blue""}]"
            //             }
            //         }
            //     }
            // }
        });


        mockUserAssetsProvider.Setup(m => m.GetUserIsDisplayTokenSymbolAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<UserTokenIndex>());
        mockUserAssetsProvider.Setup(m => m.GetUserNotDisplayTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<(string, string)>());


        mockUserAssetsProvider.Setup(m =>
            m.GetNftItemTraitsInfoAsync(It.IsAny<GetNftItemInfosDto>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new IndexerNftItemInfos());

        mockUserAssetsProvider
            .Setup(t => t.GetNftItemInfosAsync(It.IsAny<GetNftItemInfosDto>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new IndexerNftItemInfos()
            {
                NftItemInfos = new List<NftItemInfo>()
                {
                    new NftItemInfo()
                    {
                        Symbol = "MockSymbol"
                    }
                }
            });

        return mockUserAssetsProvider.Object;
    }

    private ITokenAppService GetMockTokenAppService()
    {
        var mockTokenAppService = new Mock<ITokenAppService>();

        mockTokenAppService.Setup(m => m.GetTokenHistoryPriceDataAsync(It.IsAny<List<GetTokenHistoryPriceInput>>()))
            .ReturnsAsync(
                new ListResultDto<TokenPriceDataDto>
                {
                    Items = new[]
                    {
                        new TokenPriceDataDto
                        {
                            Symbol = "ELF",
                            PriceInUsd = 2
                        }
                    }
                });

        mockTokenAppService.Setup(m => m.GetTokenPriceListAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(
                new ListResultDto<TokenPriceDataDto>
                {
                    Items = new[]
                    {
                        new TokenPriceDataDto
                        {
                            Symbol = "ELF",
                            PriceInUsd = 2
                        }
                    }
                });

        return mockTokenAppService.Object;
    }

    private IUserContactProvider GetUserContactProvider()
    {
        var mockUserContactProvider = new Mock<IUserContactProvider>();

        mockUserContactProvider.Setup(m =>
                m.BatchGetUserNameAsync(It.IsAny<List<string>>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Tuple<ContactAddress, string, string>>
            {
                new(new ContactAddress
                {
                    Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }, "test", "test")
            });

        return mockUserContactProvider.Object;
    }

    private IContractProvider GetContractProvider()
    {
        var fromBase58 = Address.FromBase58("NJRa6TYqvAgfDsLnsKXCA2jt3bYLEA8rUgPPzwMAG3YYXviHY");

        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(m =>
                m.GetHolderInfoAsync(
                    Hash.LoadFromHex("a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033"), null,
                    It.IsAny<string>()))
            .ReturnsAsync(new GetHolderInfoOutput
                {
                    CaAddress = fromBase58,
                    CaHash = Hash.LoadFromHex("a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033")
                }
            );

        return mockContractProvider.Object;
    }


    private IOptions<TokenInfoOptions> GetMockTokenInfoOptions()
    {
        var dict = new Dictionary<string, Options.TokenInfo>
        {
            ["ELF"] = new()
            {
                ImageUrl = "ImageUrl"
            }
        };

        return new OptionsWrapper<TokenInfoOptions>(new TokenInfoOptions
        {
            TokenInfos = dict
        });
    }

    private IOptions<AssetsInfoOptions> GetMockAssetsInfoOptions()
    {
        return new OptionsWrapper<AssetsInfoOptions>(new AssetsInfoOptions()
        {
            ImageUrlPrefix = "https://raw.githubusercontent.com/Portkey-Wallet/assets/master/blockchains/",
            ImageUrlSuffix = "/info/logo.png"
        });
    }

    private IOptionsSnapshot<SeedImageOptions> GetMockSeedImageOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<SeedImageOptions>>();
        var dict = new Dictionary<string, string>
        {
            ["TEST-0"] = "ImageUrl.svg"
        };

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new SeedImageOptions
            {
                SeedImageDic = dict
            });
        return mockOptionsSnapshot.Object;
    }

    private ITokenProvider GetMockTokenProvider()
    {
        var tokenProvider = new Mock<ITokenProvider>();

        tokenProvider.Setup(t => t.GetUserTokenInfoListAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UserTokenIndex>()
            {
                new UserTokenIndex()
                {
                    IsDisplay = true,
                    IsDefault = true,
                    Token = new Token()
                    {
                        Symbol = "ELF",
                        ChainId = "AELF"
                    }
                }
            });
        return tokenProvider.Object;
    }

    private ISearchAppService GetMockSearchAppService()
    {
        var page = new PagedResultDto<UserTokenIndexDto>
        {
            TotalCount = 10,
            Items = new List<UserTokenIndexDto>
            {
                new UserTokenIndexDto()
                {
                    Id = Guid.NewGuid(),
                    IsDefault = true,
                    SortWeight = 1,
                    UserId = Guid.NewGuid(),
                    Token = new Search.Dtos.Token()
                    {
                        Address = "NJRa6TYqvAgfDsLnsKXCA2jt3bYLEA8rUgPPzwMAG3YYXviHY",
                        ChainId = "AELF",
                        Decimals = 10,
                        Symbol = "SEED-0",
                        ImageUrl = "MockUrl"
                    }
                }
            }
        };
        var jsonStr = JsonConvert.SerializeObject(page);

        var searchAppService = new Mock<ISearchAppService>();

        searchAppService.Setup(t => t.GetListByLucenceAsync(It.IsAny<string>(), It.IsAny<GetListInput>()))
            .ReturnsAsync(jsonStr);

        return searchAppService.Object;
    }

    private IActivityProvider GetMockActivityProvider()
    {
        var mockActivityProvider = new Mock<IActivityProvider>();
        mockActivityProvider.Setup(t => t.GetTokenDecimalsAsync(It.IsAny<string>())).ReturnsAsync(
            new IndexerSymbols()
            {
                TokenInfo = new List<SymbolInfo>()
                {
                    new SymbolInfo()
                    {
                        ChainId = "MockChainId",
                        Decimals = 8
                    }
                }
            });
        return mockActivityProvider.Object;
    }
}