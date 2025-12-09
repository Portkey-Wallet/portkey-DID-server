using System;
using System.Collections.Generic;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity;

public partial class UserActivityAppServiceTests
{
    public static IActivityProvider GetMockActivityProvider()
    {
        var mockActivityProvider = new Mock<IActivityProvider>();

        mockActivityProvider.Setup(m => m.GetActivitiesAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new IndexerTransactions
            {
                CaHolderTransaction = new CaHolderTransaction
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerTransaction>
                    {
                        new()
                        {
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            ChainId = "AELF",
                            FromAddress = "From",
                            Id = "id",
                            MethodName = "methodName",
                            TokenInfo = null,
                            Status = "status",
                            Timestamp = 1000,
                            TransferInfo = new TransferInfo
                            {
                                FromAddress = null,
                                ToAddress = null,
                                Amount = 2,
                                ToChainId = null,
                                FromChainId = null,
                                FromCAAddress = null,
                                TransferTransactionId = null
                            },
                            TransactionId = "id",
                            TransactionFees = new List<IndexerTransactionFee>
                            {
                                new()
                                {
                                    Symbol = "ELF",
                                    Amount = 100
                                }
                            },
                            IsManagerConsumer = false,
                            NftInfo = new NftInfo()
                            {
                                Symbol = "ELF"
                            }
                        }
                    }
                }
            });

        mockActivityProvider.Setup(m => m.GetActivitiesAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(),
            It.IsAny<string>(), new List<string>() { "ContractTypes" }, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new IndexerTransactions
            {
                CaHolderTransaction = new CaHolderTransaction
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerTransaction>
                    {
                        new()
                        {
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            ChainId = "AELF",
                            FromAddress = "From",
                            Id = "id",
                            MethodName = "ContractTypes",
                            Status = "status",
                            Timestamp = 1000,
                            TransactionId = "id",
                            TransactionFees = new List<IndexerTransactionFee>
                            {
                                new()
                                {
                                    Symbol = "ELF",
                                    Amount = 100
                                }
                            }
                        }
                    }
                }
            });
        mockActivityProvider.Setup(m => m.GetActivitiesAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(),
            It.IsAny<string>(), new List<string>() { "TransferTypes" }, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new IndexerTransactions
            {
                CaHolderTransaction = new CaHolderTransaction
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerTransaction>
                    {
                        new()
                        {
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            ChainId = "AELF",
                            FromAddress = "From",
                            Id = "id",
                            MethodName = "TransferTypes",
                            Status = "status",
                            Timestamp = 1000,
                            TransactionId = "id",
                            TransactionFees = new List<IndexerTransactionFee>
                            {
                                new()
                                {
                                    Symbol = "ELF",
                                    Amount = 100
                                }
                            }
                        }
                    }
                }
            });
        mockActivityProvider
            .Setup(m => m.GetActivityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<CAAddressInfo>>()))
            .ReturnsAsync(
                new IndexerTransactions
                {
                    CaHolderTransaction = new CaHolderTransaction
                    {
                        TotalRecordCount = 1,
                        Data = new List<IndexerTransaction>
                        {
                            new()
                            {
                                BlockHash = "BlockHash",
                                BlockHeight = 100,
                                ChainId = "AELF",
                                FromAddress = "From",
                                Id = "id",
                                MethodName = "methodName",
                                Status = "status",
                                Timestamp = 1000,
                                TransactionId = "id",
                                TransactionFees = new List<IndexerTransactionFee>
                                {
                                    new()
                                    {
                                        Symbol = "ELF",
                                        Amount = 100
                                    }
                                }
                            }
                        }
                    }
                });

        mockActivityProvider.Setup(m => m.GetCaHolderNickName(It.IsAny<Guid>())).ReturnsAsync("nickname");
        mockActivityProvider.Setup(m => m.GetTokenDecimalsAsync(It.IsAny<string>())).ReturnsAsync(new IndexerSymbols
        {
            TokenInfo = new List<SymbolInfo>
            {
                new()
                {
                    Decimals = 8
                }
            }
        });

        mockActivityProvider.Setup(m => m.GetTwoCaTransactionsAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(),
            It.IsAny<List<string>>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new TransactionsDto()
        {
            TwoCaHolderTransaction = new CaHolderTransaction()
            {
                Data = new List<IndexerTransaction>(),
                TotalRecordCount = 1
            }
        });

        return mockActivityProvider.Object;
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
                m.BatchGetUserNameAsync(It.IsAny<List<string>>(), It.IsAny<Guid>(), It.IsAny<string>(),It.IsAny<string>()))
            .ReturnsAsync(new List<Tuple<ContactAddress, string, string>>
            {
                new(new ContactAddress
                {
                    Address = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                    ChainId = "AELF",
                }, "test", "test")
            });

        return mockUserContactProvider.Object;
    }

    private ITokenPriceService GetTokenPriceService()
    {
        var mock = new Mock<ITokenPriceService>();
        mock.Setup(m => m.GetCurrentPriceAsync(It.IsAny<string>())).ReturnsAsync(new TokenPriceDataDto
        {
            Symbol = "ELF",
            PriceInUsd = 5.0m
        });
        return mock.Object;
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

    private IOptions<ActivitiesIcon> GetActivitiesIcon()
    {
        return new OptionsWrapper<ActivitiesIcon>(
            new ActivitiesIcon
            {
                Transfer = "transfer",
                Contract = "contract "
            });
    }


    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();


        mockUserAssetsProvider.Setup(m => m.GetCaHolderManagerInfoAsync(It.IsAny<List<string>>())).ReturnsAsync(
            new CAHolderInfo
            {
                CaHolderManagerInfo = new List<Manager>()
                {
                    new Manager()
                    {
                        CaHash = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033",
                    }
                }
            });

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
                                }
                            }
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


        mockUserAssetsProvider.Setup(m => m.GetUserIsDisplayTokenSymbolAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<UserTokenIndex>());
        mockUserAssetsProvider.Setup(m => m.GetUserNotDisplayTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<(string, string)>());

        return mockUserAssetsProvider.Object;
    }
}