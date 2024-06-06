using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Options;
using CAServer.Search;
using CAServer.Search.Dtos;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using ChainOptions = CAServer.Options.ChainOptions;
using Token = CAServer.UserAssets.Dtos.Token;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TokenNftAppService : CAServerAppService, ITokenNftAppService
{
    private readonly ILogger<TokenDisplayAppService> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IImageProcessProvider _imageProcessProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IUserTokenAppService _userTokenAppService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly IDistributedCache<List<Token>> _userTokenCache;
    private readonly IDistributedCache<string> _userTokenBalanceCache;
    private readonly GetBalanceFromChainOption _getBalanceFromChainOption;
    private readonly ISearchAppService _searchAppService;
    private readonly IpfsOptions _ipfsOptions;
    private readonly TokenListOptions _tokenListOptions;
    private readonly IContractProvider _contractProvider;
    private readonly NftToFtOptions _nftToFtOptions;

    public TokenNftAppService(
        ILogger<TokenDisplayAppService> logger, IUserAssetsProvider userAssetsProvider,
        ITokenAppService tokenAppService,
        IImageProcessProvider imageProcessProvider, IOptions<ChainOptions> chainOptions,
        IContractProvider contractProvider, IDistributedEventBus distributedEventBus,
        IUserTokenAppService userTokenAppService, ITokenProvider tokenProvider,
        IAssetsLibraryProvider assetsLibraryProvider, IDistributedCache<List<Token>> userTokenCache,
        IDistributedCache<string> userTokenBalanceCache,
        IOptionsSnapshot<GetBalanceFromChainOption> getBalanceFromChainOption,
        ISearchAppService searchAppService, IOptionsSnapshot<IpfsOptions> ipfsOption,
        IOptionsSnapshot<TokenListOptions> tokenListOptions, IOptionsSnapshot<NftToFtOptions> nftToFtOptions)
    {
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
        _tokenAppService = tokenAppService;
        _imageProcessProvider = imageProcessProvider;
        _contractProvider = contractProvider;
        _chainOptions = chainOptions.Value;
        _distributedEventBus = distributedEventBus;
        _userTokenAppService = userTokenAppService;
        _tokenProvider = tokenProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
        _userTokenCache = userTokenCache;
        _userTokenBalanceCache = userTokenBalanceCache;
        _getBalanceFromChainOption = getBalanceFromChainOption.Value;
        _searchAppService = searchAppService;
        _ipfsOptions = ipfsOption.Value;
        _tokenListOptions = tokenListOptions.Value;
        _nftToFtOptions = nftToFtOptions.Value;
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
                0, Int32.MaxValue);

            indexerTokenInfos.CaHolderTokenBalanceInfo.Data =
                indexerTokenInfos.CaHolderTokenBalanceInfo.Data.Where(t => t.TokenInfo != null).ToList();

            var userId = CurrentUser.GetId();
            var userTokens =
                await _tokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);

            var tokenKey = $"{CommonConstant.ResourceTokenKey}:{userId.ToString()}";
            var tokenCacheList = await _userTokenCache.GetAsync(tokenKey);
            await CheckNeedAddTokenAsync(userId, indexerTokenInfos, userTokens, tokenCacheList);


            var dto = new GetTokenDto
            {
                Data = new List<Token>(),
                TotalRecordCount = 0
            };

            AddDefaultTokens(userTokens);
            var userTokenSymbols = userTokens.Where(t => t.IsDefault || t.IsDisplay).ToList();

            if (userTokenSymbols.IsNullOrEmpty())
            {
                _logger.LogError("get no result from current user {id}", userId);
                return dto;
            }

            var tokenList = new List<Token>();
            await SetNftToFtAsync(tokenList, caAddressInfos, userTokenSymbols);
            await SetFtAsync(tokenList, caAddressInfos, userTokenSymbols, indexerTokenInfos);

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

                token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol);
                tokenList.Add(token);
            }

            var symbols = tokenList.Select(t => t.Symbol).Distinct().ToList();
            dto.Data.AddRange(tokenList);
            dto.Data = SortTokens(dto.Data);
            dto.TotalRecordCount = dto.Data.Count;

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
                token.BalanceInUsd = token.Price == 0 ? string.Empty : balanceInUsd.ToString();
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

    private async Task SetFtAsync(List<Token> tokenList, List<CAAddressInfo> caAddressInfos,
        List<UserTokenIndex> userTokenSymbols, IndexerTokenInfos indexerTokenInfos)
    {
        var chainIds = _chainOptions.ChainInfos.Keys.ToList();
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
            token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol);

            tokenList.Add(token);
        }
    }

    private async Task SetNftToFtAsync(List<Token> tokenList, List<CAAddressInfo> caAddressInfos,
        List<UserTokenIndex> userTokenSymbols)
    {
        foreach (var ftInfo in _nftToFtOptions.NftToFtInfos)
        {
            var nftBalanceInfo =
                await _userAssetsProvider.GetUserNftInfoBySymbolAsync(caAddressInfos, ftInfo.Key, 0,
                    _chainOptions.ChainInfos.Keys.Count);
            var nfts = userTokenSymbols.Where(t => t.Token.Symbol == ftInfo.Key)
                .ToList();

            foreach (var nftItem in nfts)
            {
                var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(nftItem.Token.Symbol);
                var balance = nftBalanceInfo.CaHolderNFTBalanceInfo.Data?
                    .FirstOrDefault(t =>
                        t.NftInfo.Symbol == nftItem.Token.Symbol && t.ChainId == nftItem.Token.ChainId);

                var tokenInfo = new IndexerTokenInfo
                {
                    Balance = balance?.Balance ?? 0,
                    ChainId = nftItem.Token.ChainId,
                    TokenInfo = new TokenInfo
                    {
                        Decimals = nftItem.Token.Decimals,
                        Symbol = nftItem.Token.Symbol,
                        TokenContractAddress = nftItem.Token.Address
                    }
                };
                var nftToken = ObjectMapper.Map<IndexerTokenInfo, Token>(tokenInfo);
                nftToken.Label = nftToFtInfo.Label;
                nftToken.ImageUrl = nftToFtInfo.ImageUrl;
                tokenList.Add(nftToken);
            }
        }

        userTokenSymbols.RemoveAll(t => _nftToFtOptions.NftToFtInfos.Keys.Contains(t.Token.Symbol));
    }

    private string CalculateTotalBalanceInUsd(List<Token> tokens)
    {
        var totalBalanceInUsd = tokens
            .Where(token => !string.IsNullOrEmpty(token.BalanceInUsd))
            .Sum(token => decimal.Parse(token.BalanceInUsd, System.Globalization.CultureInfo.InvariantCulture));

        return totalBalanceInUsd.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }


    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        //symbol is fuzzy matching
        var chainId = input.ChainIds.Count == 1 ? input.ChainIds.First() : string.Empty;

        var userTokensDto = await _tokenProvider.GetUserTokenInfoListAsync(CurrentUser.GetId(), chainId, string.Empty);
        AddDefaultTokens(userTokensDto, input.Symbol);
        userTokensDto = userTokensDto?.Where(t => t.Token.Symbol.Contains(input.Symbol.Trim().ToUpper())).ToList();
        var indexerToken =
            await _tokenProvider.GetTokenInfosAsync(chainId, string.Empty, input.Symbol.Trim().ToUpper());

        var tokenInfoList = GetTokenInfoList(userTokensDto, indexerToken.TokenInfo);
        foreach (var nffItem in tokenInfoList.Where(t => _nftToFtOptions.NftToFtInfos.Keys.Contains(t.Symbol)))
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(nffItem.Symbol);
            if (nftToFtInfo != null)
            {
                nffItem.Label = nftToFtInfo.Label;
            }
        }
        
        // Check and adjust SkipCount and MaxResultCount
        var skipCount = input.SkipCount < TokensConstants.SkipCountDefault
            ? TokensConstants.SkipCountDefault
            : input.SkipCount;
        var maxResultCount = input.MaxResultCount <= TokensConstants.MaxResultCountInvalid
            ? TokensConstants.MaxResultCountDefault
            : input.MaxResultCount;

        return tokenInfoList.Skip(skipCount).Take(maxResultCount).ToList();
    }

    public async Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        var userPackageFtAssetsIndex = await GetUserPackageFtAssetsIndexAsync(requestDto);
        var ftTokens = userPackageFtAssetsIndex.Items.ToList();
        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();

        ftTokens.RemoveAll(t => !t.IsDisplay && sourceSymbols.Contains(t.Token.Symbol));
        AddDefaultAssertTokens(ftTokens, requestDto.Keyword);
        var userPackageAssets = await GetUserPackageAssetsAsync(requestDto);

        var userPackageFtAssetsWithPositiveBalance = userPackageAssets.Data
            .Where(asset => asset.TokenInfo?.Balance != null && long.Parse(asset.TokenInfo.Balance) > 0)
            .ToList();

        var userPackageNftAssetsWithPositiveBalance = userPackageAssets.Data
            .Where(asset => asset.NftInfo?.Balance != null
                            && long.Parse(asset.NftInfo.Balance) > 0)
            .ToList();

        var matchedItems =
            MatchAndConvertToUserPackageAssets(ftTokens, userPackageFtAssetsWithPositiveBalance);

        var unmatchedItems =
            UnMatchAndConvertToUserPackageAssets(ftTokens, userPackageFtAssetsWithPositiveBalance);

        return MergeAndBuildDto(matchedItems, ConvertToUserPackageAssets(userPackageNftAssetsWithPositiveBalance),
            unmatchedItems);
    }

    private List<UserPackageAsset> ConvertToUserPackageAssets(List<UserAsset> userPackageNftAssetsWithPositiveBalance)
    {
        var userPackageAssets = new List<UserPackageAsset>();

        foreach (var asset in userPackageNftAssetsWithPositiveBalance)
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(asset.Symbol);
            if (nftToFtInfo != null)
            {
                var ftAsset =
                    userPackageAssets.FirstOrDefault(t => t.Symbol == asset.Symbol && t.ChainId == asset.ChainId);
                if (ftAsset != null)
                {
                    ftAsset.Balance = asset.NftInfo?.Balance;
                }

                continue;
            }

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
                AssetType = (int)AssetType.NFT,
                IsDisplay = true
            };

            userPackageAssets.Add(userPackageAsset);
        }

        return userPackageAssets;
    }

    private List<UserPackageAsset> UnMatchAndConvertToUserPackageAssets(
        List<UserTokenIndexDto> ftTokens,
        List<UserAsset> userPackageFtAssetsWithPositiveBalance)
    {
        var matchedItems = ftTokens
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
                Balance = "0",
                IsDisplay = item.IsDisplay
            };

            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(item.Token.Symbol);
            if (nftToFtInfo != null)
            {
                userPackageAsset.Label = nftToFtInfo.Label;
                userPackageAsset.ImageUrl = nftToFtInfo.ImageUrl;
            }

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
        dto.Data = SortUserPackageAssets(dto.Data);

        SetSeedStatusAndTypeForUserPackageAssets(dto.Data);

        OptimizeSeedAliasDisplayForUserPackageAssets(dto.Data);

        TryUpdateImageUrlForUserPackageAssets(dto.Data);

        return dto;
    }

    private void TryUpdateImageUrlForUserPackageAssets(List<UserPackageAsset> assets)
    {
        foreach (var asset in assets)
        {
            asset.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(asset.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
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

    private List<UserPackageAsset> SortUserPackageAssets(List<UserPackageAsset> assets)
    {
        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();

        return assets.OrderBy(t => t.Symbol != CommonConstant.ELF)
            .ThenBy(t => !t.IsDisplay)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => sourceSymbols.Contains(t.Symbol))
            .ThenBy(t => t.AssetType == (int)AssetType.NFT)
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenBy(t => t.ChainId)
            .ToList();
    }

    private List<UserPackageAsset> MatchAndConvertToUserPackageAssets(
        List<UserTokenIndexDto> ftTokens,
        List<UserAsset> userPackageFtAssetsWithPositiveBalance)
    {
        var matchedItems = ftTokens
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
                Balance = correspondingAsset.TokenInfo.Balance,
                IsDisplay = item.IsDisplay
            };

            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(item.Token.Symbol);
            if (nftToFtInfo != null)
            {
                userPackageAsset.Label = nftToFtInfo.Label;
                userPackageAsset.ImageUrl = nftToFtInfo.ImageUrl;
            }

            userPackageAssets.Add(userPackageAsset);
        }

        return userPackageAssets;
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

                    tokenInfo.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(item.Symbol);

                    item.TokenInfo = tokenInfo;
                }

                if (searchItem.NftInfo != null)
                {
                    if (searchItem.NftInfo.Symbol.IsNullOrEmpty())
                    {
                        continue;
                    }

                    var nftBalance = searchItem.Balance.ToString();
                    if (_getBalanceFromChainOption.IsOpen && _getBalanceFromChainOption.Symbols.Contains(item.Symbol))
                    {
                        var correctBalance =
                            await CorrectTokenBalanceAsync(item.Symbol, searchItem.CaAddress, searchItem.ChainId);
                        nftBalance = correctBalance >= 0 ? correctBalance.ToString() : nftBalance;
                    }

                    var ftInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(searchItem.NftInfo.Symbol);
                    if (ftInfo != null)
                    {
                        item.TokenInfo = new TokenInfoDto
                        {
                            Balance = nftBalance,
                            Decimals = searchItem.NftInfo.Decimals.ToString(),
                            TokenContractAddress = searchItem.NftInfo.TokenContractAddress,
                            ImageUrl = ftInfo.ImageUrl
                        };

                        item.Label = ftInfo.Label;
                        item.NftInfo = null;
                        dto.Data.Add(item);
                        continue;
                    }

                    item.NftInfo = ObjectMapper.Map<IndexerSearchTokenNft, NftInfoDto>(searchItem);
                    item.NftInfo.Balance = nftBalance;
                    item.NftInfo.TokenId = searchItem.NftInfo.Symbol.Split("-").Last();

                    item.NftInfo.ImageUrl =
                        await _imageProcessProvider.GetResizeImageAsync(searchItem.NftInfo.ImageUrl, requestDto.Width,
                            requestDto.Height, ImageResizeType.Forest);
                    item.NftInfo.TokenName = searchItem.NftInfo.TokenName;
                }

                dto.Data.Add(item);
            }

            var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
            dto.Data = dto.Data.Where(t => t.TokenInfo != null).OrderBy(t => t.Symbol != CommonConstant.DefaultSymbol)
                .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
                .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
                .ThenBy(t => t.Symbol).ThenBy(t => t.ChainId)
                .Union(dto.Data.Where(f => f.NftInfo != null).OrderBy(e => e.Symbol).ThenBy(t => t.ChainId)).ToList();

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

    private void AddDefaultAssertTokens(List<UserTokenIndexDto> tokens, string keyword)
    {
        var defaultTokens = _tokenListOptions.UserToken;
        if (!keyword.IsNullOrEmpty())
        {
            defaultTokens = defaultTokens.Where(t => t.Token.Symbol.ToUpper().Contains(keyword.Trim().ToUpper()))
                .ToList();
        }

        foreach (var item in defaultTokens)
        {
            var token = tokens.FirstOrDefault(t =>
                t.Token.ChainId == item.Token.ChainId && t.Token.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, UserTokenIndexDto>(item));
        }
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

    private void AddDefaultTokens(List<UserTokenIndex> tokens)
    {
        foreach (var item in _tokenListOptions.UserToken)
        {
            var token = tokens.FirstOrDefault(t =>
                t.Token.ChainId == item.Token.ChainId && t.Token.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, UserTokenIndex>(item));
        }
    }

    private void AddDefaultTokens(List<UserTokenIndex> tokens, string keyword)
    {
        var userTokens = _tokenListOptions.UserToken;
        if (!keyword.IsNullOrEmpty())
        {
            userTokens =
                userTokens.Where(t => t.Token.Symbol.ToUpper().Contains(keyword.Trim().ToUpper())).ToList();
        }

        foreach (var item in userTokens)
        {
            var token = tokens.FirstOrDefault(t =>
                t.Token.ChainId == item.Token.ChainId && t.Token.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, UserTokenIndex>(item));
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

    private List<Token> SortTokens(List<Token> tokens)
    {
        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();

        return tokens.OrderBy(t => decimal.Parse(t.Balance) == 0)
            .ThenBy(t => t.Symbol != CommonConstant.ELF)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenBy(t => t.ChainId)
            .ToList();
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

    private List<GetTokenListDto> GetTokenInfoList(List<UserTokenIndex> userTokenInfos, List<IndexerToken> tokenInfos)
    {
        var result = new List<GetTokenListDto>();
        var tokenList = ObjectMapper.Map<List<IndexerToken>, List<GetTokenListDto>>(tokenInfos);
        var userTokens = ObjectMapper.Map<List<UserTokenIndex>, List<GetTokenListDto>>(userTokenInfos);
        if (tokenList.Count > 0)
        {
            tokenList.RemoveAll(t =>
                userTokens.Select(f => new { f.Symbol, f.ChainId }).Contains(new { t.Symbol, t.ChainId }));
        }

        if (userTokens.Select(t => t.IsDefault).Contains(true))
        {
            result.AddRange(userTokens.Where(t => t.IsDefault).OrderBy(t => t.ChainId));
            userTokens.RemoveAll(t => t.IsDefault);
        }

        if (userTokens.Select(t => t.IsDisplay).Contains(true))
        {
            result.AddRange(userTokens.Where(t => t.IsDisplay).OrderBy(t => t.Symbol).ThenBy(t => t.ChainId));
            userTokens.RemoveAll(t => t.IsDisplay);
        }

        userTokens.AddRange(tokenList);
        result.AddRange(userTokens.OrderBy(t => t.Symbol).ThenBy(t => t.ChainId).ToList());

        return result;
    }
}