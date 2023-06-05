using System;
using System.Collections.Generic;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity;

public partial class UserActivityAppServiceTests
{
    private IActivityProvider GetMockActivityProvider()
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

        mockActivityProvider.Setup(m => m.GetActivityAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
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

    private IOptions<ActivitiesIcon> GetActivitiesIcon()
    {
        return new OptionsWrapper<ActivitiesIcon>(
            new ActivitiesIcon
            {
                Transfer = "transfer",
                Contract = "contract "
            });
    }
}