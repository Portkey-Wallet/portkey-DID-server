using System;
using System.Collections.Generic;
using CAServer.CAActivity.Provider;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
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
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
                m.GetUserTokenInfoAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokenInfos
            {
                CaHolderTokenBalanceInfo = new CaHolderTokenBalanceInfo
                {
                    totalRecordCount = 1,
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
                        }
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
            m.GetUserNftCollectionInfoAsync(It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
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
                m.GetUserNftInfoAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
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
                }
            });

        mockUserAssetsProvider.Setup(m =>
            m.GetRecentTransactionUsersAsync(It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
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
            m.SearchUserAssetsAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new IndexerSearchTokenNfts
            {
                CaHolderSearchTokenNFT = new CaHolderSearchTokenNFT
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerSearchTokenNft>
                    {
                        new ()
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
                            }
                        }
                    }
                }
            });

        mockUserAssetsProvider.Setup(m => m.GetUserIsDisplayTokenSymbolAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<UserTokenIndex>());
        mockUserAssetsProvider.Setup(m => m.GetUserNotDisplayTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<(string, string)>());

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
                m.BatchGetUserNameAsync(It.IsAny<List<string>>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Tuple<ContactAddress, string>>
            {
                new(new ContactAddress
                {
                    Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF"
                }, "test")
            });

        return mockUserContactProvider.Object;
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
}