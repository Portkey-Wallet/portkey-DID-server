using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.FreeMint.Provider;
using CAServer.Options;
using CAServer.Search;
using CAServer.Search.Dtos;
using CAServer.Tokens;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Provider;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using ChainOptions = CAServer.Options.ChainOptions;
using Token = CAServer.UserAssets.Dtos.Token;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;
using TokenType = CAServer.Tokens.TokenType;

namespace CAServer.UserAssets;

[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : CAServerAppService, IUserAssetsAppService
{
    private readonly ILogger<UserAssetsAppService> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IUserContactProvider _userContactProvider;
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IContractProvider _contractProvider;
    private const int MaxResultCount = 10;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly SeedImageOptions _seedImageOptions;
    private readonly IUserTokenAppService _userTokenAppService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly IDistributedCache<List<Token>> _userTokenCache;
    private readonly IDistributedCache<string> _userTokenBalanceCache;
    private readonly GetBalanceFromChainOption _getBalanceFromChainOption;
    private readonly NftItemDisplayOption _nftItemDisplayOption;
    private readonly ISearchAppService _searchAppService;
    private readonly ITokenCacheProvider _tokenCacheProvider;
    private readonly IpfsOptions _ipfsOptions;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly IDistributedCache<string> _userNftTraitsCountCache;
    private const string TraitsCachePrefix = "PortKey:NFTtraits:";
    private readonly IActivityProvider _activityProvider;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly FreeMintOptions _freeMintOptions;
    private readonly IFreeMintProvider _freeMintProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAssetsAppService(
        ILogger<UserAssetsAppService> logger, IUserAssetsProvider userAssetsProvider, ITokenAppService tokenAppService,
        IUserContactProvider userContactProvider, IOptions<TokenInfoOptions> tokenInfoOptions,
        IImageProcessProvider imageProcessProvider, IOptions<ChainOptions> chainOptions,
        IContractProvider contractProvider, IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<SeedImageOptions> seedImageOptions, IUserTokenAppService userTokenAppService,
        ITokenProvider tokenProvider, IAssetsLibraryProvider assetsLibraryProvider,
        IDistributedCache<List<Token>> userTokenCache, IDistributedCache<string> userTokenBalanceCache,
        IOptionsSnapshot<GetBalanceFromChainOption> getBalanceFromChainOption,
        IOptionsSnapshot<NftItemDisplayOption> nftItemDisplayOption,
        ISearchAppService searchAppService, ITokenCacheProvider tokenCacheProvider,
        IOptionsSnapshot<IpfsOptions> ipfsOption, ITokenPriceService tokenPriceService,
        IDistributedCache<string> userNftTraitsCountCache, IActivityProvider activityProvider,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions,
        IObjectMapper objectMapper, IOptionsSnapshot<FreeMintOptions> freeMintOptions,
        IFreeMintProvider freeMintProvider, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
        _userContactProvider = userContactProvider;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _tokenAppService = tokenAppService;
        _imageProcessProvider = imageProcessProvider;
        _contractProvider = contractProvider;
        _seedImageOptions = seedImageOptions.Value;
        _chainOptions = chainOptions.Value;
        _distributedEventBus = distributedEventBus;
        _userTokenAppService = userTokenAppService;
        _tokenProvider = tokenProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
        _userTokenCache = userTokenCache;
        _userTokenBalanceCache = userTokenBalanceCache;
        _getBalanceFromChainOption = getBalanceFromChainOption.Value;
        _nftItemDisplayOption = nftItemDisplayOption.Value;
        _searchAppService = searchAppService;
        _tokenCacheProvider = tokenCacheProvider;
        _ipfsOptions = ipfsOption.Value;
        _tokenPriceService = tokenPriceService;
        _userNftTraitsCountCache = userNftTraitsCountCache;
        _activityProvider = activityProvider;
        _nftToFtOptions = nftToFtOptions.Value;
        _objectMapper = objectMapper;
        _freeMintOptions = freeMintOptions.Value;
        _freeMintProvider = freeMintProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        try
        {
            var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(CurrentUser.GetId());
            await _distributedEventBus.PublishAsync(new UserLoginEto()
            {
                Id = CurrentUser.GetId(),
                UserId = CurrentUser.GetId(),
                CaHash = caHolderIndex.CaHash,
                CreateTime = DateTime.UtcNow
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "send UserLoginEto fail,user {id}", CurrentUser.GetId());
        }

        try
        {
            var caAddressInfos = requestDto.CaAddressInfos;
            var indexerTokenInfos = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
                0, PagedResultRequestDto.MaxMaxResultCount);

            indexerTokenInfos.CaHolderTokenBalanceInfo.Data =
                indexerTokenInfos.CaHolderTokenBalanceInfo.Data.Where(t => t.TokenInfo != null).ToList();

            var userId = CurrentUser.GetId();
            var userTokens =
                await _tokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);

            var tokenKey = $"{CommonConstant.ResourceTokenKey}:{userId.ToString()}";
            var tokenCacheList = await _userTokenCache.GetAsync(tokenKey);
            await CheckNeedAddTokenAsync(userId, indexerTokenInfos, userTokens, tokenCacheList);

            var chainInfos =
                await _userAssetsProvider.GetUserChainIdsAsync(requestDto.CaAddressInfos.Select(t => t.CaAddress)
                    .ToList());
            var chainIds = chainInfos.CaHolderManagerInfo.Select(c => c.ChainId).Distinct().ToList();

            var dto = new GetTokenDto
            {
                Data = new List<Token>(),
                TotalRecordCount = 0
            };

            var userTokenSymbols = userTokens.Where(t => t.IsDefault || t.IsDisplay).ToList();
            if (userTokenSymbols.IsNullOrEmpty())
            {
                _logger.LogError("get no result from current user {id}", userId);
                return dto;
            }

            var tokenList = new List<Token>();
            foreach (var symbol in userTokenSymbols)
            {
                if (!chainIds.Contains(symbol.Token.ChainId))
                {
                    continue;
                }

                var tokenInfo = indexerTokenInfos.CaHolderTokenBalanceInfo.Data.FirstOrDefault(t =>
                    t.TokenInfo.Symbol == symbol.Token.Symbol && t.ChainId == symbol.Token.ChainId);

                if (tokenInfo == null)
                {
                    var data = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos,
                        symbol.Token.Symbol, 0, caAddressInfos.Count);
                    tokenInfo = data.CaHolderTokenBalanceInfo.Data.FirstOrDefault(
                        t => t.ChainId == symbol.Token.ChainId);
                    tokenInfo ??= new IndexerTokenInfo
                    {
                        Balance = 0,
                        ChainId = symbol.Token.ChainId,
                        TokenInfo = new TokenInfo
                        {
                            Decimals = symbol.Token.Decimals,
                            Symbol = symbol.Token.Symbol,
                            TokenContractAddress = symbol.Token.Address
                        }
                    };
                }
                else
                {
                    indexerTokenInfos.CaHolderTokenBalanceInfo.Data.Remove(tokenInfo);
                }

                var token = ObjectMapper.Map<IndexerTokenInfo, Token>(tokenInfo);
                token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol, token.ImageUrl);

                tokenList.Add(token);
            }

            var userTokensWithBalance =
                ObjectMapper.Map<List<IndexerTokenInfo>, List<Token>>(indexerTokenInfos.CaHolderTokenBalanceInfo.Data);


            foreach (var token in userTokensWithBalance)
            {
                var tokenCache =
                    tokenCacheList?.FirstOrDefault(t => t.ChainId == token.ChainId && t.Symbol == token.Symbol);
                if (tokenCache != null)
                {
                    continue;
                }

                token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol, token.ImageUrl);
                tokenList.Add(token);
            }

            dto.TotalRecordCount = tokenList.Count;
            tokenList = tokenList.OrderBy(t => t.Symbol).ThenBy(t => t.ChainId).ToList();
            var defaultList = tokenList.Where(t => t.Symbol == CommonConstant.DefaultSymbol).ToList();

            var resultList = defaultList.Union(tokenList).ToList();
            var symbols = resultList.Select(t => t.Symbol).ToList();
            dto.Data.AddRange(resultList);

            if (_getBalanceFromChainOption.IsOpen)
            {
                foreach (var token in dto.Data)
                {
                    if (_getBalanceFromChainOption.Symbols.Contains(token.Symbol))
                    {
                        var caAddressInfo = requestDto.CaAddressInfos.FirstOrDefault(t => t.ChainId == token.ChainId);
                        if (caAddressInfo == null)
                        {
                            continue;
                        }

                        var correctBalance = await CorrectTokenBalanceAsync(token.Symbol,
                            caAddressInfo.CaAddress, token.ChainId);
                        token.Balance = correctBalance >= 0 ? correctBalance.ToString() : token.Balance;
                    }
                }
            }

            var priceDict = await GetSymbolPrice(symbols);
            foreach (var token in dto.Data)
            {
                if (!priceDict.ContainsKey(token.Symbol))
                {
                    continue;
                }

                var balanceInUsd = CalculationHelper.GetBalanceInUsd(priceDict[token.Symbol], long.Parse(token.Balance),
                    token.Decimals);
                token.Price = priceDict[token.Symbol];
                token.BalanceInUsd = balanceInUsd.ToString();
            }

            dto.TotalBalanceInUsd = CalculateTotalBalanceInUsd(dto.Data);
            dto.Data = dto.Data.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTokenAsync Error. {dto}", requestDto);
            return new GetTokenDto { Data = new List<Token>(), TotalRecordCount = 0 };
        }
    }

    private string CalculateTotalBalanceInUsd(List<Token> tokens)
    {
        var totalBalanceInUsd = tokens
            .Where(token => !string.IsNullOrEmpty(token.BalanceInUsd))
            .Sum(token => decimal.Parse(token.BalanceInUsd, System.Globalization.CultureInfo.InvariantCulture));

        return totalBalanceInUsd.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task CheckNeedAddTokenAsync(Guid userId, IndexerTokenInfos tokenInfos,
        List<UserTokenIndex> userTokens, List<Token> tokenCacheList)
    {
        try
        {
            var tokens = tokenInfos?.CaHolderTokenBalanceInfo?.Data?.Where(t => t.TokenInfo != null && t.Balance > 0)
                .Select(f => f.TokenInfo)
                .ToList();

            if (tokens.IsNullOrEmpty()) return;

            var tokenIds = new List<string>();
            foreach (var token in tokens)
            {
                var cacheToken =
                    tokenCacheList?.FirstOrDefault(f => f.Symbol == token.Symbol && f.ChainId == token.ChainId);
                if (cacheToken != null)
                {
                    continue;
                }

                var userToken =
                    userTokens.FirstOrDefault(f => f.Token.Symbol == token.Symbol && f.Token.ChainId == token.ChainId);
                if (userToken == null || !userToken.IsDisplay)
                {
                    var tokenId = userToken != null ? userToken.Id.ToString() : token.Id;
                    tokenIds.Add(tokenId);
                }
            }

            await AddDisplayAsync(tokenIds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "check need add token failed, userId: {userId}", userId);
        }
    }

    private async Task AddDisplayAsync(List<string> tokenIds)
    {
        var tasks = new List<Task>();
        foreach (var tokenId in tokenIds)
        {
            tasks.Add(_userTokenAppService.ChangeTokenDisplayAsync(true, tokenId));
        }

        await Task.WhenAll(tasks);
    }

    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetUserNftCollectionInfoAsync(requestDto.CaAddressInfos,
                0, LimitedResultRequestDto.MaxMaxResultCount);

            var dto = new GetNftCollectionsDto
            {
                Data = new List<NftCollection>(),
                TotalRecordCount = res?.CaHolderNFTCollectionBalanceInfo?.TotalRecordCount ?? 0
            };

            if (res?.CaHolderNFTCollectionBalanceInfo?.Data == null ||
                res.CaHolderNFTCollectionBalanceInfo.Data.Count == 0)
            {
                _logger.LogInformation("[GetNFTCollectionsAsync] data from indexer is empty.");
                return dto;
            }

            var totalItemCount = res.CaHolderNFTCollectionBalanceInfo.Data.SelectMany(item => item.TokenIds).Count();
            res.CaHolderNFTCollectionBalanceInfo.Data = res.CaHolderNFTCollectionBalanceInfo.Data
                .Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();

            foreach (var nftCollectionInfo in res.CaHolderNFTCollectionBalanceInfo.Data)
            {
                var nftCollection =
                    ObjectMapper.Map<IndexerNftCollectionInfo, NftCollection>(nftCollectionInfo);
                if (nftCollectionInfo?.NftCollectionInfo == null)
                {
                    dto.Data.Add(nftCollection);
                }
                else
                {
                    var isInSeedOption =
                        _seedImageOptions.SeedImageDic.TryGetValue(nftCollection.Symbol, out var imageUrl);
                    var image = isInSeedOption ? imageUrl : nftCollectionInfo.NftCollectionInfo.ImageUrl;
                    nftCollection.ImageUrl = await _imageProcessProvider.GetResizeImageAsync(
                        image, requestDto.Width, requestDto.Height,
                        ImageResizeType.Forest);
                    dto.Data.Add(nftCollection);
                }
            }

            SetSeedStatusAndTrimCollectionNameForCollections(dto.Data);

            TryUpdateImageUrlForCollections(dto.Data);

            DealWithDisplayChainImage(dto);
            dto.TotalNftItemCount = totalItemCount;
            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTCollectionsAsync Error. {dto}", requestDto);
            return new GetNftCollectionsDto { Data = new List<NftCollection>(), TotalRecordCount = 0 };
        }
    }

    private static void DealWithDisplayChainImage(GetNftCollectionsDto dto)
    {
        var symbolToCount = dto.Data.GroupBy(nft => nft.Symbol)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionary(g => g.GroupId, g => g.Count);
        foreach (var nftCollection in dto.Data)
        {
            symbolToCount.TryGetValue(nftCollection.Symbol, out var count);
            nftCollection.DisplayChainImage = count > 1;
        }
    }

    public async Task<SearchUserAssetsV2Dto> SearchUserAssetsAsyncV2(SearchUserAssetsRequestDto requestDto,
        SearchUserAssetsDto searchDto)
    {
        var nftRequestDto = _objectMapper.Map<SearchUserAssetsRequestDto, GetNftCollectionsRequestDto>(requestDto);
        var collectionsDto = await GetNFTCollectionsAsync(nftRequestDto);
        return SearchUserAssetsHelper.ToSearchV2(searchDto, collectionsDto.Data, _objectMapper);
    }

    private void SetSeedStatusAndTrimCollectionNameForCollections(List<NftCollection> collections)
    {
        foreach (var collection in collections)
        {
            // If Symbol is null or empty, skip this iteration
            if (string.IsNullOrEmpty(collection.Symbol))
            {
                continue;
            }

            // If Symbol starts with "SEED-", set IsSeed to true, otherwise set it to false
            collection.IsSeed = collection.Symbol.StartsWith(TokensConstants.SeedNamePrefix);
        }
    }

    private void TryUpdateImageUrlForCollections(List<NftCollection> collections)
    {
        foreach (var collection in collections)
        {
            collection.ImageUrl =
                IpfsImageUrlHelper.TryGetIpfsImageUrl(collection.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        }
    }

    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetUserNftInfoAsync(requestDto.CaAddressInfos,
                requestDto.Symbol, requestDto.SkipCount, requestDto.MaxResultCount);

            var dto = new GetNftItemsDto
            {
                Data = new List<NftItem>(),
                TotalRecordCount = res?.CaHolderNFTBalanceInfo?.TotalRecordCount ?? 0
            };

            if (res?.CaHolderNFTBalanceInfo?.Data == null || res.CaHolderNFTBalanceInfo.Data.Count == 0)
            {
                return dto;
            }

            var symbolToDescription = await ExtractDescription(res);
            foreach (var nftInfo in res.CaHolderNFTBalanceInfo.Data.Where(n => n.NftInfo != null))
            {
                if (nftInfo.NftInfo.Symbol.IsNullOrEmpty())
                {
                    continue;
                }

                var nftItem = ObjectMapper.Map<IndexerNftInfo, NftItem>(nftInfo);

                nftItem.TokenId = nftInfo.NftInfo.Symbol.Split("-").Last();
                nftItem.TotalSupply = nftInfo.NftInfo.TotalSupply;
                nftItem.CirculatingSupply = nftInfo.NftInfo.Supply;
                nftItem.Decimals = nftInfo.NftInfo.Decimals.ToString();
                nftItem.ImageUrl =
                    await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl, requestDto.Width,
                        requestDto.Height,
                        ImageResizeType.Forest);
                nftItem.ImageLargeUrl = await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl,
                    (int)ImageResizeWidthType.IMAGE_WIDTH_TYPE_ONE, (int)ImageResizeHeightType.IMAGE_HEIGHT_TYPE_AUTO,
                    ImageResizeType.Forest);
                nftItem.TokenName = nftInfo.NftInfo.TokenName;
                nftItem.RecommendedRefreshSeconds = _nftItemDisplayOption.RecommendedRefreshSeconds <= 0
                    ? NftItemDisplayOption.DefaultRecommendedRefreshSeconds
                    : _nftItemDisplayOption.RecommendedRefreshSeconds;

                nftItem.CollectionSymbol = nftInfo.NftInfo.CollectionSymbol;
                nftItem.InscriptionName = nftInfo.NftInfo.InscriptionName;
                nftItem.LimitPerMint = nftInfo.NftInfo.Lim;
                nftItem.Expires = nftInfo.NftInfo.Expires;
                nftItem.SeedOwnedSymbol = nftInfo.NftInfo.SeedOwnedSymbol;

                nftItem.Generation = nftInfo.NftInfo.Generation;
                nftItem.Traits = nftInfo.NftInfo.Traits;
                if (symbolToDescription.TryGetValue(nftInfo.NftInfo.Symbol, out var description))
                {
                    nftItem.Description = description;
                }

                dto.Data.Add(nftItem);
            }

            if (_getBalanceFromChainOption.IsOpen)
            {
                foreach (var nftItem in dto.Data)
                {
                    if (_getBalanceFromChainOption.Symbols.Contains(nftItem.Symbol))
                    {
                        var correctBalance = await CorrectTokenBalanceAsync(nftItem.Symbol,
                            requestDto.CaAddressInfos.First(t => t.ChainId == nftItem.ChainId).CaAddress,
                            nftItem.ChainId);
                        nftItem.Balance = correctBalance >= 0 ? correctBalance.ToString() : nftItem.Balance;
                    }
                }
            }

            SetSeedStatusAndTypeForNftItems(dto.Data);

            OptimizeSeedAliasDisplayForNftItems(dto.Data);

            TryUpdateLimitPerMintForInscription(dto.Data);

            TryUpdateImageUrlForNftItems(dto.Data);

            await TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(dto.Data);

            CalculateAndSetTraitsPercentageAsync(dto.Data);

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTItemsAsync Error. {dto}", requestDto);
            return new GetNftItemsDto { Data = new List<NftItem>(), TotalRecordCount = 0 };
        }
    }

    private async Task<Dictionary<string, string>> ExtractDescription(IndexerNftInfos res)
    {
        var nftItemSymbols = res.CaHolderNFTBalanceInfo.Data
            .Where(nftInfo => nftInfo.NftInfo != null &&
                              _freeMintOptions.CollectionInfo.CollectionName.Equals(nftInfo.NftInfo.CollectionName))
            .Select(d => d.NftInfo.Symbol).ToList();
        var freeMintIndices = await _freeMintProvider.ListFreeMintItemsAsync(nftItemSymbols);
        try
        {
            var symbolToDescription = freeMintIndices.ToDictionary(index => index.Symbol, index => index.Description);
            return symbolToDescription;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "freeMintIndices.ToDictionary error");
            return new Dictionary<string, string>();
        }
    }

    public async Task<NftItem> GetNFTItemAsync(GetNftItemRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetUserNftInfoBySymbolAsync(requestDto.CaAddressInfos,
                requestDto.Symbol, 0, 1000);

            if (res?.CaHolderNFTBalanceInfo?.Data == null || res.CaHolderNFTBalanceInfo.Data.Count == 0)
            {
                return null;
            }

            var nftInfo = res.CaHolderNFTBalanceInfo.Data
                .FirstOrDefault(n => n.NftInfo != null && !n.NftInfo.Symbol.IsNullOrEmpty());
            if (nftInfo == null)
            {
                return null;
            }

            var nftItem = ObjectMapper.Map<IndexerNftInfo, NftItem>(nftInfo);

            nftItem.TokenId = nftInfo.NftInfo.Symbol.Split("-").Last();
            nftItem.TotalSupply = nftInfo.NftInfo.TotalSupply;
            nftItem.CirculatingSupply = nftInfo.NftInfo.Supply;
            nftItem.Decimals = nftInfo.NftInfo.Decimals.ToString();
            nftItem.ImageUrl =
                await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl, requestDto.Width,
                    requestDto.Height,
                    ImageResizeType.Forest);
            nftItem.ImageLargeUrl = await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl,
                (int)ImageResizeWidthType.IMAGE_WIDTH_TYPE_ONE, (int)ImageResizeHeightType.IMAGE_HEIGHT_TYPE_AUTO,
                ImageResizeType.Forest);
            nftItem.TokenName = nftInfo.NftInfo.TokenName;
            nftItem.RecommendedRefreshSeconds = _nftItemDisplayOption.RecommendedRefreshSeconds <= 0
                ? NftItemDisplayOption.DefaultRecommendedRefreshSeconds
                : _nftItemDisplayOption.RecommendedRefreshSeconds;

            nftItem.CollectionSymbol = nftInfo.NftInfo.CollectionSymbol;
            nftItem.InscriptionName = nftInfo.NftInfo.InscriptionName;
            nftItem.LimitPerMint = nftInfo.NftInfo.Lim;
            nftItem.Expires = nftInfo.NftInfo.Expires;
            nftItem.SeedOwnedSymbol = nftInfo.NftInfo.SeedOwnedSymbol;

            nftItem.Generation = nftInfo.NftInfo.Generation;
            nftItem.Traits = nftInfo.NftInfo.Traits;

            nftItem.CollectionSymbol = nftInfo.NftInfo.CollectionSymbol;
            var freeMintIndices = await _freeMintProvider.ListFreeMintItemsBySymbolAsync(nftInfo.NftInfo.Symbol);
            nftItem.Description = freeMintIndices.IsNullOrEmpty() ? "" : freeMintIndices.FirstOrDefault()?.Description;
            if (_getBalanceFromChainOption.IsOpen && _getBalanceFromChainOption.Symbols.Contains(nftItem.Symbol))
            {
                var correctBalance = await CorrectTokenBalanceAsync(nftItem.Symbol,
                    requestDto.CaAddressInfos.First(t => t.ChainId == nftItem.ChainId).CaAddress, nftItem.ChainId);
                nftItem.Balance = correctBalance >= 0 ? correctBalance.ToString() : nftItem.Balance;
            }

            SetSeedStatusAndTypeForNftItem(nftItem);

            OptimizeSeedAliasDisplayForNftItem(nftItem);

            TryUpdateLimitPerMintForInscription(nftItem);

            TryUpdateImageUrlForNftItem(nftItem);

            await TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(nftItem);

            await CalculateAndSetTraitsPercentageAsync(nftItem);

            return nftItem;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTItemAsync Error. {dto}", requestDto);
            return null;
        }
    }

    private void SetSeedStatusAndTypeForNftItems(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            SetSeedStatusAndTypeForNftItem(nftItem);
        }
    }

    private void SetSeedStatusAndTypeForNftItem(NftItem nftItem)
    {
        // If the Symbol starts with "SEED", we set IsSeed to true.
        if (nftItem.Symbol.StartsWith(TokensConstants.SeedNamePrefix))
        {
            nftItem.IsSeed = true;
            nftItem.SeedType = (int)SeedType.FT;

            if (!string.IsNullOrEmpty(nftItem.SeedOwnedSymbol))
            {
                nftItem.SeedType = nftItem.SeedOwnedSymbol.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
            }

            // Compatible with historical data
            // If the TokenName starts with "SEED-", we remove "SEED-" and check if it contains "-"
            else if (!string.IsNullOrEmpty(nftItem.TokenName) &&
                     nftItem.TokenName.StartsWith(TokensConstants.SeedNamePrefix))
            {
                var tokenNameWithoutSeed = nftItem.TokenName.Remove(0, 5);

                // If TokenName contains "-", set SeedType to NFT, otherwise set it to FT
                nftItem.SeedType = tokenNameWithoutSeed.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
            }
        }
    }


    private void OptimizeSeedAliasDisplayForNftItems(List<NftItem> nftItems)
    {
        foreach (var item in nftItems)
        {
            OptimizeSeedAliasDisplayForNftItem(item);
        }
    }

    private void OptimizeSeedAliasDisplayForNftItem(NftItem nftItem)
    {
        if (nftItem.IsSeed && nftItem.Alias.EndsWith(TokensConstants.SeedAliasNameSuffix))
        {
            nftItem.Alias = nftItem.Alias.TrimEnd(TokensConstants.SeedAliasNameSuffix.ToCharArray());
        }
    }

    private void TryUpdateLimitPerMintForInscription(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            TryUpdateLimitPerMintForInscription(nftItem);
        }
    }

    private void TryUpdateLimitPerMintForInscription(NftItem nftItem)
    {
        if (!string.IsNullOrEmpty(nftItem.LimitPerMint) && nftItem.LimitPerMint.Equals("0"))
        {
            nftItem.LimitPerMint = TokensConstants.LimitPerMintReplacement;
        }
    }

    private void TryUpdateImageUrlForNftItems(List<NftItem> nftItems)
    {
        foreach (var nftItem in nftItems)
        {
            TryUpdateImageUrlForNftItem(nftItem);
        }
    }

    private void TryUpdateImageUrlForNftItem(NftItem nftItem)
    {
        nftItem.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(nftItem.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        nftItem.ImageLargeUrl =
            IpfsImageUrlHelper.TryGetIpfsImageUrl(nftItem.ImageLargeUrl, _ipfsOptions?.ReplacedIpfsPrefix);
    }

    private async Task TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(List<NftItem> nftItems)
    {
        foreach (var item in nftItems)
        {
            await TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(item);
        }
    }

    private async Task TryGetSeedAttributeValueFromContractIfEmptyForSeedAsync(NftItem nftItem)
    {
        if (nftItem.IsSeed && (string.IsNullOrEmpty(nftItem.Expires) || string.IsNullOrEmpty(nftItem.SeedOwnedSymbol)))
        {
            var nftItemCache =
                await _tokenCacheProvider.GetTokenInfoAsync(nftItem.ChainId, nftItem.Symbol, TokenType.NFTItem);
            nftItem.Expires = nftItemCache.Expires;
            nftItem.SeedOwnedSymbol = nftItemCache.SeedOwnedSymbol;
        }
    }

    private void CalculateAndSetTraitsPercentageAsync(List<NftItem> nftItems)
    {
        foreach (var item in nftItems.Where(item => !string.IsNullOrEmpty(item.Traits)))
        {
            item.TraitsPercentages = new List<Trait>();
        }
    }


    private async Task CalculateAndSetTraitsPercentageAsync(NftItem nftItem)
    {
        if (!string.IsNullOrEmpty(nftItem.Traits))
        {
            var traitsList = JsonHelper.DeserializeJson<List<Trait>>(nftItem.Traits);
            if (traitsList == null || !traitsList.Any())
            {
                nftItem.TraitsPercentages = new List<Trait>();
            }

            await CalculateTraitsPercentagesAsync(nftItem, traitsList);
        }
    }


    private async Task<IndexerNftItemInfos> GetNftItemTraitsInfoAsync()
    {
        var itemInfos = new IndexerNftItemInfos
        {
            NftItemInfos = new List<NftItemInfo>()
        };
        var skipCount = 0;
        const int resultCount = 2000;
        var symbol = string.Empty;
        var collectionSymbol = CommonConstant.SgrCollectionSymbol;
        while (true)
        {
            var nftItemInfos =
                await _userAssetsProvider.GetNftItemWithTraitsInfos(symbol, collectionSymbol, skipCount, resultCount);
            if (nftItemInfos?.NftItemWithTraitsInfos?.Count == 0 || nftItemInfos?.NftItemWithTraitsInfos == null)
            {
                break;
            }

            skipCount += resultCount;
            symbol = nftItemInfos.NftItemWithTraitsInfos.LastOrDefault()?.Symbol;
            _logger.LogInformation("[GetNftItemTraitsInfoAsync] Next symbol: {symbol}", symbol);
            var list = nftItemInfos.NftItemWithTraitsInfos;
            if (list != null)
            {
                itemInfos.NftItemInfos.AddRange(list);
            }
        }

        _logger.LogInformation("[GetNftItemTraitsInfoAsync] TotalCount of NftItems is {count}",
            itemInfos.NftItemInfos.Count);
        return itemInfos;
    }

    private async Task CalculateTraitsPercentagesAsync(NftItem nftItem, List<Trait> traitsList)
    {
        foreach (var trait in traitsList)
        {
            var traitType = trait.TraitType;
            var traitTypeValue = $"{trait.TraitType}-{trait.Value}";

            var traitsTyperCount = await _userNftTraitsCountCache.GetAsync(TraitsCachePrefix + traitType);
            var traitsTypeValueCount = await _userNftTraitsCountCache.GetAsync(TraitsCachePrefix + traitTypeValue);
            _logger.LogInformation(
                "CalculateTraitsPercentagesAsync traitsTyperCount key = {0} value = {1} ; traitsTypeValueCount key = {2} value = {3}",
                TraitsCachePrefix + traitType, traitsTyperCount, TraitsCachePrefix + traitTypeValue,
                traitsTypeValueCount);

            if (traitsTyperCount != null && traitsTypeValueCount != null)
            {
                var percentage =
                    PercentageHelper.CalculatePercentage(int.Parse(traitsTypeValueCount), int.Parse(traitsTyperCount));
                trait.Percent = percentage;
            }
            else
            {
                trait.Percent = "-";
            }
        }

        nftItem.TraitsPercentages = traitsList;
    }


    //Data with the same name needs to be deduplicated
    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(
        GetRecentTransactionUsersRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetRecentTransactionUsersAsync(requestDto.CaAddressInfos,
                requestDto.SkipCount, requestDto.MaxResultCount);

            var dto = new GetRecentTransactionUsersDto
            {
                Data = new List<RecentTransactionUser>(),
                TotalRecordCount = res?.CaHolderTransactionAddressInfo?.TotalRecordCount ?? 0
            };

            if (res?.CaHolderTransactionAddressInfo?.Data == null || res.CaHolderTransactionAddressInfo.Data.Count == 0)
            {
                return dto;
            }

            var userCaAddresses = res.CaHolderTransactionAddressInfo.Data.Select(t => t.Address)?.Distinct()?.ToList();

            foreach (var info in res.CaHolderTransactionAddressInfo.Data)
            {
                dto.Data.Add(ObjectMapper.Map<CAHolderTransactionAddress, RecentTransactionUser>(info));
            }

            var contactList = await _userContactProvider.BatchGetUserNameAsync(userCaAddresses, CurrentUser.GetId(),
                _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString());
            if (contactList == null)
            {
                return dto;
            }

            var addressList = contactList.Select(t => t.Item1.Address).ToList();
            foreach (var user in dto.Data.Where(user => addressList.Contains(user.Address)))
            {
                var contact =
                    contactList?.OrderBy(t => GetIndex(t.Item2)).FirstOrDefault(t =>
                        t.Item1.Address == user.Address && t.Item1.ChainId == user.AddressChainId);

                user.Name = contact?.Item2;
                user.Avatar = contact?.Item3;
                user.Index = GetIndex(user.Name);

                if (!string.IsNullOrWhiteSpace(user.Name))
                {
                    await AssembleAddressesAsync(user);
                }
            }
            //  At this time, maybe there is data in the list with the same name but different address

            var users = GetDuplicatedUser(dto.Data);

            dto.Data = users;
            dto.TotalRecordCount = res.CaHolderTransactionAddressInfo.TotalRecordCount;

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRecentTransactionUsersAsync Error. {dto}", requestDto);
            return new GetRecentTransactionUsersDto { Data = new List<RecentTransactionUser>(), TotalRecordCount = 0 };
        }
    }

    // Get a list of all addresses below according to name（same name,same address list）
    private async Task AssembleAddressesAsync(RecentTransactionUser user)
    {
        var contactAddresses =
            await _userContactProvider.GetContactByUserNameAsync(user.Name, CurrentUser.GetId(),
                _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString());
        user.Addresses = ObjectMapper.Map<List<ContactAddress>, List<UserContactAddressDto>>(contactAddresses);
        user.Addresses?.ForEach(t =>
        {
            if (t.ChainId == user.AddressChainId && t.Address == user.Address)
            {
                t.TransactionTime = user.TransactionTime;
            }
        });
    }

    //Deduplicate data with same name，And put the TransactionTime of the corresponding address list in the position of the corresponding address list of the unremoved name, Then sort according to the time
    private List<RecentTransactionUser> GetDuplicatedUser(List<RecentTransactionUser> users)
    {
        var userDic = new Dictionary<string, RecentTransactionUser>();
        var result = new List<RecentTransactionUser>();
        if (users == null)
        {
            return result;
        }

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                result.Add(user);
                continue;
            }

            if (userDic.ContainsKey(user.Name))
            {
                var contactAddressDto =
                    user.Addresses.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.TransactionTime));

                if (contactAddressDto == null) continue;

                var preContactAddressDto = userDic[user.Name].Addresses.First(t =>
                    t.ChainId == contactAddressDto.ChainId && t.Address == contactAddressDto.Address);

                preContactAddressDto.TransactionTime = contactAddressDto.TransactionTime;
            }
            else
            {
                userDic.Add(user.Name, user);
                result.Add(user);
            }
        }

        users.ForEach(t =>
        {
            t.Addresses ??= new List<UserContactAddressDto>();
            t.Addresses = t.Addresses
                .OrderByDescending(f => string.IsNullOrEmpty(f.TransactionTime) ? 0 : long.Parse(f.TransactionTime))
                .ToList();
        });

        return result;
    }

    private string GetIndex(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "#";

        var firstChar = char.ToUpperInvariant(name[0]);
        if (firstChar >= 'A' && firstChar <= 'Z')
        {
            return firstChar.ToString();
        }

        return "#";
    }

    public async Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.SearchUserAssetsAsync(requestDto.CaAddressInfos,
                requestDto.Keyword.IsNullOrEmpty() ? "" : requestDto.Keyword,
                requestDto.SkipCount, requestDto.MaxResultCount);

            var dto = new SearchUserAssetsDto
            {
                Data = new List<UserAsset>(),
                TotalRecordCount = res?.CaHolderSearchTokenNFT?.TotalRecordCount ?? 0
            };

            if (res?.CaHolderSearchTokenNFT?.Data == null || res.CaHolderSearchTokenNFT.Data.Count == 0)
            {
                return dto;
            }

            var symbols = (from searchItem in res.CaHolderSearchTokenNFT.Data
                where searchItem.TokenInfo != null
                select searchItem.TokenInfo.Symbol).ToList();
            var symbolPrices = await GetSymbolPrice(symbols);
            foreach (var searchItem in res.CaHolderSearchTokenNFT.Data)
            {
                var item = ObjectMapper.Map<IndexerSearchTokenNft, UserAsset>(searchItem);
                if (searchItem == null || (searchItem.NftInfo == null && searchItem.TokenInfo == null))
                {
                    continue;
                }

                if (searchItem.TokenInfo != null)
                {
                    var price = decimal.Zero;
                    if (symbolPrices.ContainsKey(item.Symbol))
                    {
                        price = symbolPrices[item.Symbol];
                    }

                    var tokenInfo = ObjectMapper.Map<IndexerSearchTokenNft, TokenInfoDto>(searchItem);
                    if (_getBalanceFromChainOption.IsOpen && _getBalanceFromChainOption.Symbols.Contains(item.Symbol))
                    {
                        var correctBalance =
                            await CorrectTokenBalanceAsync(item.Symbol, searchItem.CaAddress, searchItem.ChainId);
                        if (correctBalance >= 0)
                        {
                            searchItem.Balance = correctBalance;
                            tokenInfo.Balance = correctBalance.ToString();
                        }
                    }

                    tokenInfo.BalanceInUsd = tokenInfo.BalanceInUsd = CalculationHelper
                        .GetBalanceInUsd(price, searchItem.Balance, Convert.ToInt32(tokenInfo.Decimals)).ToString();

                    tokenInfo.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(item.Symbol, tokenInfo.ImageUrl);

                    item.TokenInfo = tokenInfo;
                }

                if (searchItem.NftInfo != null)
                {
                    if (searchItem.NftInfo.Symbol.IsNullOrEmpty())
                    {
                        continue;
                    }

                    item.NftInfo = ObjectMapper.Map<IndexerSearchTokenNft, NftInfoDto>(searchItem);
                    if (_getBalanceFromChainOption.IsOpen && _getBalanceFromChainOption.Symbols.Contains(item.Symbol))
                    {
                        var correctBalance =
                            await CorrectTokenBalanceAsync(item.Symbol, searchItem.CaAddress, searchItem.ChainId);
                        item.NftInfo.Balance = correctBalance >= 0 ? correctBalance.ToString() : item.NftInfo.Balance;
                    }

                    item.NftInfo.TokenId = searchItem.NftInfo.Symbol.Split("-").Last();

                    item.NftInfo.ImageUrl =
                        await _imageProcessProvider.GetResizeImageAsync(searchItem.NftInfo.ImageUrl, requestDto.Width,
                            requestDto.Height, ImageResizeType.Forest);
                    item.NftInfo.TokenName = searchItem.NftInfo.TokenName;
                    item.NftInfo.Symbol = searchItem.NftInfo.Symbol;
                }

                dto.Data.Add(item);
            }

            dto.Data = dto.Data.Where(t => t.TokenInfo != null).OrderBy(t => t.Symbol != CommonConstant.DefaultSymbol)
                .ThenBy(t => t.Symbol).ThenByDescending(t => t.ChainId)
                .Union(dto.Data.Where(f => f.NftInfo != null).OrderBy(e => e.Symbol).ThenByDescending(t => t.ChainId))
                .ToList();

            SetSeedStatusAndTypeForUserAssets(dto.Data);

            OptimizeSeedAliasDisplayForUserAssets(dto.Data);

            TryUpdateImageUrlForUserAssets(dto.Data);

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SearchUserAssetsAsync Error. {dto}", requestDto);
            return new SearchUserAssetsDto { Data = new List<UserAsset>(), TotalRecordCount = 0 };
        }
    }

    private void SetSeedStatusAndTypeForUserAssets(List<UserAsset> userAssets)
    {
        foreach (var userAsset in userAssets)
        {
            // If Symbol starts with "SEED", set IsSeed to true
            if (userAsset.Symbol.StartsWith(TokensConstants.SeedNamePrefix) && userAsset.NftInfo != null)
            {
                userAsset.NftInfo.IsSeed = true;
                userAsset.NftInfo.SeedType = (int)SeedType.FT;

                // If the TokenName is not null and starts with "SEED-", we remove "SEED-" and check if it contains "-"
                if (!string.IsNullOrEmpty(userAsset.NftInfo.TokenName) &&
                    userAsset.NftInfo.TokenName.StartsWith(TokensConstants.SeedNamePrefix))
                {
                    var tokenNameWithoutSeed = userAsset.NftInfo.TokenName.Remove(0, 5);

                    // If TokenName contains "-", set SeedType to NFT, otherwise set it to FT
                    userAsset.NftInfo.SeedType =
                        tokenNameWithoutSeed.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
                }
            }
        }
    }

    private void OptimizeSeedAliasDisplayForUserAssets(List<UserAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.NftInfo == null)
            {
                continue;
            }

            if (asset.NftInfo.IsSeed && asset.NftInfo.Alias.EndsWith(TokensConstants.SeedAliasNameSuffix))
            {
                asset.NftInfo.Alias = asset.NftInfo.Alias.TrimEnd(TokensConstants.SeedAliasNameSuffix.ToCharArray());
            }
        }
    }

    private void TryUpdateImageUrlForUserAssets(List<UserAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.NftInfo == null)
            {
                continue;
            }

            asset.NftInfo.ImageUrl =
                IpfsImageUrlHelper.TryGetIpfsImageUrl(asset.NftInfo.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        }
    }

    public async Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        var userPackageFtAssetsIndex = await GetUserPackageFtAssetsIndexAsync(requestDto);

        var userPackageAssets = await GetUserPackageAssetsAsync(requestDto);

        var userPackageFtAssetsWithPositiveBalance = userPackageAssets.Data
            .Where(asset => asset.TokenInfo?.Balance != null && long.Parse(asset.TokenInfo.Balance) > 0)
            .ToList();

        var userPackageNftAssetsWithPositiveBalance = userPackageAssets.Data
            .Where(asset => asset.NftInfo?.Balance != null
                            && long.Parse(asset.NftInfo.Balance) > 0)
            .ToList();

        var matchedItems =
            MatchAndConvertToUserPackageAssets(userPackageFtAssetsIndex, userPackageFtAssetsWithPositiveBalance);

        var unmatchedItems =
            UnMatchAndConvertToUserPackageAssets(userPackageFtAssetsIndex, userPackageFtAssetsWithPositiveBalance);

        return MergeAndBuildDto(matchedItems, ConvertToUserPackageAssets(userPackageNftAssetsWithPositiveBalance),
            unmatchedItems);
    }

    private List<UserPackageAsset> MatchAndConvertToUserPackageAssets(
        PagedResultDto<UserTokenIndexDto> userPackageFtAssetsIndex,
        List<UserAsset> userPackageFtAssetsWithPositiveBalance)
    {
        var matchedItems = userPackageFtAssetsIndex.Items
            .Where(item => userPackageFtAssetsWithPositiveBalance.Any(asset =>
                asset.ChainId == item.Token.ChainId && asset.Symbol == item.Token.Symbol))
            .ToList();

        var userPackageAssets = new List<UserPackageAsset>();

        foreach (var item in matchedItems)
        {
            var correspondingAsset = userPackageFtAssetsWithPositiveBalance.First(asset =>
                asset.ChainId == item.Token.ChainId && asset.Symbol == item.Token.Symbol);

            var userPackageAsset = new UserPackageAsset
            {
                ChainId = item.Token.ChainId,
                Symbol = item.Token.Symbol,
                Decimals = item.Token.Decimals.ToString(),
                ImageUrl = item.Token.ImageUrl,
                AssetType = (int)AssetType.FT,
                TokenContractAddress = item.Token.Address,
                Balance = correspondingAsset.TokenInfo.Balance
            };

            userPackageAssets.Add(userPackageAsset);
        }

        return userPackageAssets;
    }

    private List<UserPackageAsset> UnMatchAndConvertToUserPackageAssets(
        PagedResultDto<UserTokenIndexDto> userPackageFtAssetsIndex,
        List<UserAsset> userPackageFtAssetsWithPositiveBalance)
    {
        var matchedItems = userPackageFtAssetsIndex.Items
            .Where(item => userPackageFtAssetsWithPositiveBalance.All(asset =>
                !(asset.ChainId == item.Token.ChainId && asset.Symbol == item.Token.Symbol)))
            .ToList();

        var userPackageAssets = new List<UserPackageAsset>();

        foreach (var item in matchedItems)
        {
            var userPackageAsset = new UserPackageAsset
            {
                ChainId = item.Token.ChainId,
                Symbol = item.Token.Symbol,
                Decimals = item.Token.Decimals.ToString(),
                ImageUrl = item.Token.ImageUrl,
                AssetType = (int)AssetType.FT,
                TokenContractAddress = item.Token.Address,
                Balance = "0"
            };

            userPackageAssets.Add(userPackageAsset);
        }

        return userPackageAssets;
    }

    private List<UserPackageAsset> ConvertToUserPackageAssets(List<UserAsset> userPackageNftAssetsWithPositiveBalance)
    {
        var userPackageAssets = new List<UserPackageAsset>();

        foreach (var asset in userPackageNftAssetsWithPositiveBalance)
        {
            var userPackageAsset = new UserPackageAsset
            {
                ChainId = asset.ChainId,
                Symbol = asset.Symbol,
                Decimals = asset.NftInfo?.Decimals,
                ImageUrl = asset.NftInfo?.ImageUrl,
                Alias = asset.NftInfo?.Alias,
                TokenId = asset.NftInfo?.TokenId,
                Balance = asset.NftInfo?.Balance,
                TokenContractAddress = asset.NftInfo?.TokenContractAddress,
                TokenName = asset.NftInfo?.TokenName,
                AssetType = (int)AssetType.NFT
            };

            userPackageAssets.Add(userPackageAsset);
        }

        return userPackageAssets;
    }

    private SearchUserPackageAssetsDto MergeAndBuildDto(
        List<UserPackageAsset> userPackageFtAssetsWithPositiveBalance,
        List<UserPackageAsset> userPackageNftAssetsWithPositiveBalance,
        List<UserPackageAsset> userPackageFtAssetsWithNoBalance)
    {
        var dto = new SearchUserPackageAssetsDto
        {
            TotalRecordCount = userPackageFtAssetsWithPositiveBalance.Count +
                               userPackageNftAssetsWithPositiveBalance.Count + userPackageFtAssetsWithNoBalance.Count,
            FtRecordCount = userPackageFtAssetsWithPositiveBalance.Count + userPackageFtAssetsWithNoBalance.Count,
            NftRecordCount = userPackageNftAssetsWithPositiveBalance.Count,
            Data = new List<UserPackageAsset>()
        };

        dto.Data.AddRange(userPackageFtAssetsWithPositiveBalance);
        dto.Data.AddRange(userPackageNftAssetsWithPositiveBalance);
        dto.Data.AddRange(userPackageFtAssetsWithNoBalance);

        SetSeedStatusAndTypeForUserPackageAssets(dto.Data);

        OptimizeSeedAliasDisplayForUserPackageAssets(dto.Data);

        TryUpdateImageUrlForUserPackageAssets(dto.Data);

        return dto;
    }

    private void SetSeedStatusAndTypeForUserPackageAssets(List<UserPackageAsset> userPackageAssets)
    {
        foreach (var userPackageAsset in userPackageAssets)
        {
            // If AssetType is NFT and Symbol starts with "SEED", set IsSeed to true
            if (userPackageAsset.AssetType == (int)AssetType.NFT &&
                userPackageAsset.Symbol.StartsWith(TokensConstants.SeedNamePrefix))
            {
                userPackageAsset.IsSeed = true;
                userPackageAsset.SeedType = (int)SeedType.FT;

                // If the TokenName is not null and starts with "SEED-", we remove "SEED-" and check if it contains "-"
                if (!string.IsNullOrEmpty(userPackageAsset.TokenName) &&
                    userPackageAsset.TokenName.StartsWith(TokensConstants.SeedNamePrefix))
                {
                    var tokenNameWithoutSeed = userPackageAsset.TokenName.Remove(0, 5);

                    // If TokenName contains "-", set SeedType to NFT, otherwise set it to FT
                    userPackageAsset.SeedType =
                        tokenNameWithoutSeed.Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
                }
            }
        }
    }

    private void OptimizeSeedAliasDisplayForUserPackageAssets(List<UserPackageAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.IsSeed && asset.Alias.EndsWith(TokensConstants.SeedAliasNameSuffix))
            {
                asset.Alias = asset.Alias.TrimEnd(TokensConstants.SeedAliasNameSuffix.ToCharArray());
            }
        }
    }

    private void TryUpdateImageUrlForUserPackageAssets(List<UserPackageAsset> assets)
    {
        foreach (var asset in assets)
        {
            asset.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(asset.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
        }
    }

    private async Task<PagedResultDto<UserTokenIndexDto>> GetUserPackageFtAssetsIndexAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        var chainIds = requestDto.CaAddressInfos.Select(t => t.ChainId).ToList();
        var chainIdsFilter = chainIds.Aggregate((current, next) =>
            string.Join(" OR ", $"token.chainId:{current}", $"token.chainId:{next}"));
        var keyword = requestDto.Keyword.IsNullOrEmpty() ? "" : requestDto.Keyword.ToUpper();
        var input = new GetListInput
        {
            Filter = $"token.symbol: *{keyword}* AND ({chainIdsFilter})",
            Sort = "sortWeight desc,isDisplay desc,token.symbol acs,token.chainId acs",
            MaxResultCount = requestDto.MaxResultCount,
            SkipCount = requestDto.SkipCount,
        };

        return JsonConvert.DeserializeObject<PagedResultDto<UserTokenIndexDto>>(
            await _searchAppService.GetListByLucenceAsync("usertokenindex", input), new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
    }

    private async Task<SearchUserAssetsDto> GetUserPackageAssetsAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        SearchUserAssetsRequestDto input = new SearchUserAssetsRequestDto
        {
            CaAddressInfos = requestDto.CaAddressInfos,
            Keyword = requestDto.Keyword,
            SkipCount = requestDto.SkipCount,
            MaxResultCount = requestDto.MaxResultCount
        };

        return await SearchUserAssetsAsync(input);
    }


    public SymbolImagesDto GetSymbolImagesAsync()
    {
        var dto = new SymbolImagesDto { SymbolImages = new Dictionary<string, string>() };

        if (_tokenInfoOptions.TokenInfos.IsNullOrEmpty())
        {
            return dto;
        }

        dto.SymbolImages = _tokenInfoOptions.TokenInfos.ToDictionary(k => k.Key, v => v.Value.ImageUrl);

        return dto;
    }

    public async Task<TokenInfoDto> GetTokenBalanceAsync(GetTokenBalanceRequestDto requestDto)
    {
        var caAddress = new List<string>
        {
            requestDto.CaAddress
        };
        var result = await _userAssetsProvider.GetCaHolderManagerInfoAsync(caAddress);
        if (result == null || result.CaHolderManagerInfo.IsNullOrEmpty())
        {
            return new TokenInfoDto();
        }

        var caHash = result.CaHolderManagerInfo.First().CaHash;
        var caAddressInfos = new List<CAAddressInfo>();
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            if (!string.IsNullOrEmpty(requestDto.ChainId) && !requestDto.ChainId.Equals(chainInfo.Value.ChainId))
            {
                continue;
            }

            try
            {
                var output =
                    await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainInfo.Value.ChainId);
                caAddressInfos.Add(new CAAddressInfo
                {
                    ChainId = chainInfo.Key,
                    CaAddress = output.CaAddress.ToBase58()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetTokenBalanceAsync Error. CaAddress is {CaAddress}", requestDto.CaAddress);
            }
        }

        if (caAddressInfos.IsNullOrEmpty())
        {
            _logger.LogDebug("No caAddressInfos. CaAddress is {CaAddress}", requestDto.CaAddress);
            return new TokenInfoDto();
        }

        var res = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, requestDto.Symbol,
            0, MaxResultCount);
        var resCaHolderTokenBalanceInfo = res.CaHolderTokenBalanceInfo.Data;
        var totalBalance = resCaHolderTokenBalanceInfo.Sum(tokenInfo => tokenInfo.Balance);

        var totalBalanceInUsd = await CalculateTotalBalanceInUsdAsync(resCaHolderTokenBalanceInfo);

        return new TokenInfoDto
        {
            Balance = totalBalance > 0 ? totalBalance.ToString() : "0",
            Decimals = resCaHolderTokenBalanceInfo.First().TokenInfo.Decimals.ToString(),
            BalanceInUsd = totalBalanceInUsd.ToString()
        };
    }

    public async Task NftTraitsProportionCalculateAsync()
    {
        var itemInfos = await GetNftItemTraitsInfoAsync();
        var allItemsTraitsListInCollection = itemInfos.NftItemInfos?
            .Where(nftItem =>
                nftItem.Supply > 0 && !string.IsNullOrEmpty(nftItem.Traits) && IsValidJson(nftItem.Traits))
            .GroupBy(nftItem => nftItem.Symbol)
            .Select(group => group.First().Traits)
            .ToList() ?? new List<string>();

        var allItemsTraitsList = allItemsTraitsListInCollection
            .Select(traits => { return JsonHelper.DeserializeJson<List<Trait>>(traits); })
            .Where(curTraitsList => curTraitsList != null && curTraitsList.Any())
            .SelectMany(curTraitsList => curTraitsList)
            .ToList();

        var traitTypeCounts = allItemsTraitsList.GroupBy(t => t.TraitType).ToDictionary(g => g.Key, g => g.Count());
        _logger.LogInformation(
            "[GetNftItemTraitsInfoAsync] NftTraitsProportionCalculateAsync traitTypeCounts length = {0}",
            traitTypeCounts.Count);
        foreach (var traits in traitTypeCounts.Keys)
        {
            await _userNftTraitsCountCache.SetAsync(TraitsCachePrefix + traits, traitTypeCounts[traits].ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                });
        }


        var traitTypeValueCounts = allItemsTraitsList.GroupBy(t => $"{t.TraitType}-{t.Value}")
            .ToDictionary(g => g.Key, g => g.Count());
        _logger.LogInformation(
            "[GetNftItemTraitsInfoAsync] NftTraitsProportionCalculateAsync traitTypeValueCounts length = {0}",
            traitTypeValueCounts.Count);
        foreach (var traitsValues in traitTypeValueCounts.Keys)
        {
            await _userNftTraitsCountCache.SetAsync(TraitsCachePrefix + traitsValues,
                traitTypeValueCounts[traitsValues].ToString(), new DistributedCacheEntryOptions()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                });
        }
    }

    public async Task<bool> UserAssetEstimationAsync(UserAssetEstimationRequestDto request)
    {
        if (request.Type == "token" && _nftToFtOptions.NftToFtInfos.ContainsKey(request.Symbol))
        {
            request.Type = "nft";
        }

        switch (request.Type)
        {
            case "token":
            {
                var token = await _activityProvider.GetTokenDecimalsAsync(request.Symbol);
                var symbolInfos = token.TokenInfo.Where(t => t.ChainId == request.ChainId).ToList();
                if (symbolInfos.Count > 0)
                {
                    return true;
                }

                break;
            }
            case "nft":
            {
                var param = new GetNftItemInfosDto
                {
                    GetNftItemInfos = new List<GetNftItemInfo>
                    {
                        new GetNftItemInfo
                        {
                            ChainId = request.ChainId,
                            Symbol = request.Symbol
                        }
                    }
                };
                var nftItemInfos = await _userAssetsProvider.GetNftItemInfosAsync(param, 0, 10);
                if (nftItemInfos.NftItemInfos.Count > 0)
                {
                    return true;
                }

                break;
            }
            default:
                return false;
        }

        return false;
    }

    private async Task<decimal> CalculateTotalBalanceInUsdAsync(List<IndexerTokenInfo> tokenInfos)
    {
        var totalBalanceInUsd = 0m;
        foreach (var tokenInfo in tokenInfos)
        {
            if (tokenInfo == null)
            {
                continue;
            }

            var currentTokenPrice = await GetCurrentTokenPriceAsync(tokenInfo.TokenInfo.Symbol);
            totalBalanceInUsd +=
                GetCurrentPriceInUsd(tokenInfo.Balance, tokenInfo.TokenInfo.Decimals, currentTokenPrice);
        }

        return totalBalanceInUsd;
    }

    private async Task<decimal> GetCurrentTokenPriceAsync(string symbol)
    {
        var priceResult = await _tokenPriceService.GetCurrentPriceAsync(symbol);
        return priceResult?.PriceInUsd ?? 0;
    }

    private decimal GetCurrentPriceInUsd(long tokenBalance, int tokenDecimals, decimal currentBalanceInUsd)
    {
        if (decimal.TryParse(tokenBalance.ToString(), out var amount))
        {
            var baseValue = (decimal)Math.Pow(10, tokenDecimals);
            return amount / baseValue * currentBalanceInUsd;
        }

        throw new ArgumentException("Invalid input values");
    }

    private async Task<Dictionary<string, decimal>> GetSymbolPrice(List<string> symbols)
    {
        try
        {
            var priceList = await _tokenAppService.GetTokenPriceListAsync(symbols);
            var dict = new Dictionary<string, decimal>();
            if (priceList == null)
            {
                return dict;
            }

            foreach (var price in priceList.Items)
            {
                dict[price.Symbol.ToUpper()] = price.PriceInUsd;
            }

            return dict;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get symbols price failed, symbol={symbols}", symbols);
            return new Dictionary<string, decimal>();
        }
    }

    private async Task<long> CorrectTokenBalanceAsync(string symbol, string address, string chainId)
    {
        var cacheKey = string.Format(CommonConstant.CacheCorrectUserTokenBalancePre, chainId, address, symbol);
        try
        {
            var userTokenBalanceCache = await _userTokenBalanceCache.GetAsync(cacheKey);
            if (string.IsNullOrWhiteSpace(userTokenBalanceCache))
            {
                var output = await _contractProvider.GetBalanceAsync(symbol, address, chainId);
                await _userTokenBalanceCache.SetAsync(cacheKey, output.Balance.ToString(),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                            _getBalanceFromChainOption?.ExpireSeconds ??
                            CommonConstant.CacheTokenBalanceExpirationSeconds)
                    });
                return output.Balance;
            }

            return long.Parse(userTokenBalanceCache);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CorrectTokenBalance fail: symbol={symbol}, address={address}, chainId={chainId}",
                symbol, address, chainId);
            return -1;
        }
    }


    private bool IsValidJson(string strInput)
    {
        try
        {
            var json = JToken.Parse(strInput);
            return true;
        }
        catch
        {
            return false;
        }
    }
}