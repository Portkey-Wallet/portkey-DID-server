using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Security.Dtos;
using CAServer.Security.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurity;
using CAServer.UserSecurity.Provider;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;
using Xunit;
using ChainConfiguration = CAServer.Options.ChainInfo;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

namespace CAServer.Security;

public class UserSecurityAssetPaginationTests
{
    private readonly string _caHash = HashHelper.ComputeFrom("asset-pagination-test").ToHex();
    private readonly Mock<IUserAssetsProvider> _assetsProvider = new(MockBehavior.Strict);
    private readonly Mock<IUserSecurityProvider> _securityProvider = new();
    private readonly Mock<IDistributedEventBus> _eventBus = new();
    private readonly List<int> _requestedOffsets = new();
    private readonly List<UserTransferLimitHistoryEto> _publishedHistory = new();
    private readonly UserSecurityAppService _service;

    public UserSecurityAssetPaginationTests()
    {
        var contractProvider = new Mock<IContractProvider>();
        var caAddress = Address.FromPublicKey(new byte[] { 1, 2, 3 });
        contractProvider.Setup(p => p.GetHolderInfoAsync(It.IsAny<Hash>(), null, It.IsAny<string>()))
            .ReturnsAsync(new GetHolderInfoOutput
            {
                CaAddress = caAddress,
                CreateChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                GuardianList = new GuardianList()
            });

        _securityProvider.Setup(p => p.GetTransferLimitListByCaHashAsync(_caHash))
            .ReturnsAsync(new IndexerTransferLimitList
            {
                CaHolderTransferLimit = new CaHolderTransferLimit
                {
                    TotalRecordCount = 0,
                    Data = new List<TransferLimitDto>()
                }
            });
        _eventBus.Setup(b => b.PublishAsync(It.IsAny<UserTransferLimitHistoryEto>(), It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Callback<UserTransferLimitHistoryEto, bool, bool>((history, _, _) => _publishedHistory.Add(history))
            .Returns(Task.CompletedTask);

        var securityOptions = new SecurityOptions
        {
            DefaultTokenTransferLimit = 1000,
            TokenBalanceTransferThreshold = new Dictionary<string, long> { ["ELF"] = 100 },
            TokenTransferLimitDict = new Dictionary<string, TokenTransferLimit>
            {
                ["AELF"] = DefaultLimits("20000", "30000"),
                ["tDVV"] = DefaultLimits("40000", "50000")
            }
        };
        var chainOptions = new ChainOptions
        {
            ChainInfos = new Dictionary<string, ChainConfiguration>
            {
                ["AELF"] = new() { ChainId = "AELF" },
                ["tDVV"] = new() { ChainId = "tDVV" }
            }
        };
        var lazyProvider = new Mock<IAbpLazyServiceProvider>();
        lazyProvider.Setup(p => p.LazyGetService<IGuidGenerator>(SimpleGuidGenerator.Instance))
            .Returns(SimpleGuidGenerator.Instance);
        _service = new UserSecurityAppService(Snapshot(securityOptions), _securityProvider.Object,
            Snapshot(chainOptions), contractProvider.Object, NullLogger<UserSecurityAppService>.Instance,
            _assetsProvider.Object, _eventBus.Object, Mock.Of<IAssetsLibraryProvider>())
        {
            LazyServiceProvider = lazyProvider.Object
        };
    }

    [Fact]
    public async Task TransferLimits_IncludeTokensAfterTwoHundredNfts()
    {
        var assets = Nfts(200);
        assets.Add(Token("AELF", "ELF"));
        assets.Add(Token("tDVV", "ELF"));
        assets.Add(Token("tDVV", "USDT"));
        SetAssets(assets);

        var result = await GetLimitsAsync();

        Assert.Equal(3, result.TotalRecordCount);
        Assert.Equal(new[] { "AELF-ELF", "tDVV-ELF", "tDVV-USDT" }, Keys(result));
        Assert.Equal(new[] { 0, 200 }, _requestedOffsets);
        Assert.Equal(3, _publishedHistory.Count);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(73)]
    public async Task TransferLimits_ReadEveryPageBeforeSortingAndResponsePagination(int returnedPageSize)
    {
        var assets = Enumerable.Range(0, 450).Select(i => Token("AELF", $"T{i:D3}")).ToList();
        assets.Add(Token("tDVV", "ELF"));
        assets.Add(Token("AELF", "ELF"));
        SetAssets(assets, returnedPageSize);

        var all = await GetLimitsAsync();

        Assert.Equal(452, all.TotalRecordCount);
        Assert.Equal(452, all.Data.Count);
        Assert.Equal(new[] { "AELF-ELF", "tDVV-ELF", "AELF-T000" }, Keys(all).Take(3));
        Assert.Equal("T449", all.Data.Last().Symbol);
        Assert.Equal(Enumerable.Range(0, (452 + returnedPageSize - 1) / returnedPageSize)
            .Select(i => i * returnedPageSize), _requestedOffsets);

        var page = await GetLimitsAsync(skip: 1, take: 3);

        Assert.Equal(452, page.TotalRecordCount);
        Assert.Equal(new[] { "tDVV-ELF", "AELF-T000", "AELF-T001" }, Keys(page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(201)]
    public async Task TransferLimits_EmptyAndNftOnlyAssetsFinishWithoutHistory(int nftCount)
    {
        SetAssets(Nfts(nftCount));

        var result = await GetLimitsAsync();

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Data);
        Assert.Empty(_publishedHistory);
        Assert.Equal(nftCount > 200 ? new[] { 0, 200 } : new[] { 0 }, _requestedOffsets);
        _securityProvider.Verify(p => p.GetUserTransferLimitHistoryAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransferLimits_SmallAccountUsesOneRequestAndExistingDefaultLimits()
    {
        SetAssets(new List<IndexerSearchTokenNft> { Token("tDVV", "AIBOUNTY"), Token("tDVV", "ELF") });

        var result = await GetLimitsAsync();

        Assert.Equal(2, result.TotalRecordCount);
        Assert.Equal(new[] { "tDVV-ELF", "tDVV-AIBOUNTY" }, Keys(result));
        Assert.Equal("40000", result.Data[0].SingleLimit);
        Assert.Equal("50000", result.Data[0].DailyLimit);
        Assert.Equal("1000", result.Data[1].SingleLimit);
        Assert.Equal("1000", result.Data[1].DailyLimit);
        Assert.Equal(new[] { 0 }, _requestedOffsets);
    }

    [Fact]
    public async Task TransferLimits_CustomLimitsOverrideOnlyTheMatchingChainAndSymbol()
    {
        var assets = Nfts(200);
        assets.Add(Token("AELF", "ELF"));
        assets.Add(Token("tDVV", "ELF"));
        SetAssets(assets);
        _securityProvider.Setup(p => p.GetTransferLimitListByCaHashAsync(_caHash))
            .ReturnsAsync(new IndexerTransferLimitList
            {
                CaHolderTransferLimit = new CaHolderTransferLimit
                {
                    TotalRecordCount = 1,
                    Data = new List<TransferLimitDto>
                    {
                        new() { ChainId = "AELF", Symbol = "ELF", SingleLimit = "-1", DailyLimit = "-1" }
                    }
                }
            });

        var result = await GetLimitsAsync();

        Assert.Equal(2, result.TotalRecordCount);
        var mainChain = result.Data.Single(t => t.ChainId == "AELF");
        Assert.Equal("-1", mainChain.SingleLimit);
        Assert.Equal("-1", mainChain.DailyLimit);
        Assert.Equal("20000", mainChain.DefaultSingleLimit);
        Assert.Equal("30000", mainChain.DefaultDailyLimit);
        Assert.False(mainChain.Restricted);
        var sideChain = result.Data.Single(t => t.ChainId == "tDVV");
        Assert.Equal("40000", sideChain.SingleLimit);
        Assert.Equal("50000", sideChain.DailyLimit);
        Assert.True(sideChain.Restricted);
    }

    [Fact]
    public async Task TransferLimits_ZeroBalanceRequiresMatchingHistoryAndDoesNotPublishAgain()
    {
        var assets = Nfts(200);
        assets.Add(Token("AELF", "ELF", 0));
        assets.Add(Token("tDVV", "ELF", 0));
        assets.Add(Token("AELF", "USDT", 0));
        SetAssets(assets);
        _securityProvider.Setup(p => p.GetUserTransferLimitHistoryAsync(_caHash, "AELF", "ELF"))
            .ReturnsAsync(new UserTransferLimitHistoryIndex { ChainId = "AELF", Symbol = "ELF" });
        _securityProvider.Setup(p => p.GetUserTransferLimitHistoryAsync(_caHash, "tDVV", "ELF"))
            .ReturnsAsync(new UserTransferLimitHistoryIndex { ChainId = "AELF", Symbol = "ELF" });

        var result = await GetLimitsAsync();

        Assert.Equal(1, result.TotalRecordCount);
        Assert.Equal(new[] { "AELF-ELF" }, Keys(result));
        Assert.Empty(_publishedHistory);
    }

    [Theory]
    [InlineData("exception")]
    [InlineData("null-response")]
    [InlineData("null-result")]
    [InlineData("null-data")]
    [InlineData("empty-page")]
    public async Task TransferLimits_IncompleteLaterPageFailsWithoutReturningOrPublishingPartialAssets(string failure)
    {
        SetIncompleteAssets(failure);

        await Assert.ThrowsAsync<UserFriendlyException>(() => GetLimitsAsync());

        Assert.Equal(new[] { 0, 200 }, _requestedOffsets);
        Assert.Empty(_publishedHistory);
        _securityProvider.Verify(p => p.GetTransferLimitListByCaHashAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(BalanceCheckPath.WithoutChain, 101, false)]
    [InlineData(BalanceCheckPath.WithoutChain, 100, true)]
    [InlineData(BalanceCheckPath.DestinationChain, 101, false)]
    [InlineData(BalanceCheckPath.DestinationChain, 100, false)]
    [InlineData(BalanceCheckPath.DestinationChain, 99, true)]
    [InlineData(BalanceCheckPath.OriginChain, 101, false)]
    [InlineData(BalanceCheckPath.OriginChain, 100, false)]
    [InlineData(BalanceCheckPath.OriginChain, 99, true)]
    public async Task BalanceCheck_ReadsLaterTokensAndPreservesThresholdBoundaries(
        BalanceCheckPath path, long balance, bool safe)
    {
        var assets = Nfts(200);
        assets.Add(Token("AELF", "ELF", balance));
        SetAssets(assets);

        var result = await CheckBalanceAsync(path);

        Assert.Equal(safe, result.IsTransferSafe);
        Assert.Equal(safe, result.IsOriginChainSafe);
        Assert.Equal(new[] { 0, 200 }, _requestedOffsets);
    }

    [Theory]
    [InlineData(BalanceCheckPath.WithoutChain)]
    [InlineData(BalanceCheckPath.DestinationChain)]
    [InlineData(BalanceCheckPath.OriginChain)]
    public async Task BalanceCheck_EmptyAssetsRemainSafe(BalanceCheckPath path)
    {
        SetAssets(new List<IndexerSearchTokenNft>());

        var result = await CheckBalanceAsync(path);

        Assert.True(result.IsTransferSafe);
        Assert.True(result.IsOriginChainSafe);
        Assert.Equal(new[] { 0 }, _requestedOffsets);
    }

    [Theory]
    [InlineData(BalanceCheckPath.WithoutChain, "exception")]
    [InlineData(BalanceCheckPath.DestinationChain, "exception")]
    [InlineData(BalanceCheckPath.OriginChain, "exception")]
    [InlineData(BalanceCheckPath.WithoutChain, "null-response")]
    [InlineData(BalanceCheckPath.DestinationChain, "null-response")]
    [InlineData(BalanceCheckPath.OriginChain, "null-response")]
    [InlineData(BalanceCheckPath.WithoutChain, "empty-page")]
    [InlineData(BalanceCheckPath.DestinationChain, "empty-page")]
    [InlineData(BalanceCheckPath.OriginChain, "empty-page")]
    public async Task BalanceCheck_IncompleteLaterPageFailsClosed(BalanceCheckPath path, string failure)
    {
        SetIncompleteAssets(failure);

        var result = await CheckBalanceAsync(path);

        Assert.False(result.IsTransferSafe);
        Assert.False(result.IsOriginChainSafe);
        Assert.False(result.IsSynchronizing);
        Assert.Equal(new[] { 0, 200 }, _requestedOffsets);
    }

    private void SetAssets(List<IndexerSearchTokenNft> assets, int returnedPageSize = 200)
    {
        SetupSearch((skip, count) => Page(assets.Skip(skip).Take(Math.Min(count, returnedPageSize)).ToList(),
            assets.Count));
    }

    private void SetIncompleteAssets(string failure)
    {
        var firstPage = Nfts(199);
        firstPage.Add(Token("AELF", "USDT"));
        SetupSearch((skip, _) => skip == 0 ? Page(firstPage, 201) : failure switch
        {
            "exception" => throw new InvalidOperationException("Indexer request failed"),
            "null-response" => null,
            "null-result" => new IndexerSearchTokenNfts(),
            "null-data" => Page(null, 201),
            "empty-page" => Page(new List<IndexerSearchTokenNft>(), 201),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        });
    }

    private void SetupSearch(Func<int, int, IndexerSearchTokenNfts> response)
    {
        _assetsProvider.Setup(p => p.SearchUserAssetsAsync(It.IsAny<List<CAAddressInfo>>(), "",
                It.IsAny<int>(), 200))
            .ReturnsAsync((List<CAAddressInfo> addresses, string _, int skip, int count) =>
            {
                Assert.Equal(new[] { "AELF", "tDVV" }, addresses.Select(a => a.ChainId));
                Assert.All(addresses, a => Assert.False(string.IsNullOrEmpty(a.CaAddress)));
                _requestedOffsets.Add(skip);
                Assert.True(_requestedOffsets.Count <= 20, "Asset pagination did not terminate");
                return response(skip, count);
            });
    }

    private Task<TransferLimitListResultDto> GetLimitsAsync(int skip = 0, int take = 1000) =>
        _service.GetTransferLimitListByCaHashAsync(new GetTransferLimitListByCaHashDto
        {
            CaHash = _caHash, SkipCount = skip, MaxResultCount = take
        });

    private Task<TokenBalanceTransferCheckAsyncResultDto> CheckBalanceAsync(BalanceCheckPath path) =>
        path == BalanceCheckPath.WithoutChain
            ? _service.GetTokenBalanceTransferCheckAsync(new GetTokenBalanceTransferCheckDto { CaHash = _caHash })
            : _service.GetTokenBalanceTransferCheckAsync(new GetTokenBalanceTransferCheckWithChainIdDto
            {
                CaHash = _caHash,
                CheckTransferSafeChainId = path == BalanceCheckPath.OriginChain ? "AELF" : "tDVV"
            });

    private static string[] Keys(TransferLimitListResultDto result) =>
        result.Data.Select(t => t.ChainId + "-" + t.Symbol).ToArray();

    private static List<IndexerSearchTokenNft> Nfts(int count) =>
        Enumerable.Range(0, count).Select(i => new IndexerSearchTokenNft
        {
            ChainId = "AELF", Balance = 1, TokenId = i, NftInfo = new NftInfo { Symbol = $"NFT-{i}" }
        }).ToList();

    private static IndexerSearchTokenNft Token(string chainId, string symbol, long balance = 1) =>
        new() { ChainId = chainId, Balance = balance, TokenInfo = new TokenInfo { Symbol = symbol, Decimals = 8 } };

    private static IndexerSearchTokenNfts Page(List<IndexerSearchTokenNft> data, long total) =>
        new() { CaHolderSearchTokenNFT = new CaHolderSearchTokenNFT { Data = data, TotalRecordCount = total } };

    private static TokenTransferLimit DefaultLimits(string single, string daily) => new()
    {
        SingleTransferLimit = new Dictionary<string, string> { ["ELF"] = single },
        DailyTransferLimit = new Dictionary<string, string> { ["ELF"] = daily }
    };

    private static IOptionsSnapshot<T> Snapshot<T>(T options) where T : class
    {
        var snapshot = new Mock<IOptionsSnapshot<T>>();
        snapshot.Setup(o => o.Value).Returns(options);
        return snapshot.Object;
    }

    public enum BalanceCheckPath
    {
        WithoutChain,
        DestinationChain,
        OriginChain
    }
}
