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
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using ChainOptions = CAServer.Options.ChainOptions;
using Token = CAServer.UserAssets.Dtos.Token;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

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

    public UserAssetsAppService(
        ILogger<UserAssetsAppService> logger, IUserAssetsProvider userAssetsProvider, ITokenAppService tokenAppService,
        IUserContactProvider userContactProvider, IOptions<TokenInfoOptions> tokenInfoOptions,
        IImageProcessProvider imageProcessProvider, IOptions<ChainOptions> chainOptions,
        IContractProvider contractProvider, IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<SeedImageOptions> seedImageOptions, IUserTokenAppService userTokenAppService,
        ITokenProvider tokenProvider, IAssetsLibraryProvider assetsLibraryProvider,
        IDistributedCache<List<Token>> userTokenCache)
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
            if (caAddressInfos == null)
            {
                caAddressInfos = requestDto.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var indexerTokenInfos = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
                0, requestDto.SkipCount + requestDto.MaxResultCount);

            indexerTokenInfos.CaHolderTokenBalanceInfo.Data =
                indexerTokenInfos.CaHolderTokenBalanceInfo.Data.Where(t => t.TokenInfo != null).ToList();

            var userId = CurrentUser.GetId();
            var userTokens =
                await _tokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);

            await CheckNeedAddTokenAsync(userId, indexerTokenInfos, userTokens);
            var chainInfos = await _userAssetsProvider.GetUserChainIdsAsync(requestDto.CaAddresses);
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
                        symbol.Token.Symbol, 0, requestDto.CaAddresses.Count);
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

            var userTokensWithBalance =
                ObjectMapper.Map<List<IndexerTokenInfo>, List<Token>>(indexerTokenInfos.CaHolderTokenBalanceInfo.Data);

            var tokenKey = $"{CommonConstant.ResourceTokenKey}:{userId.ToString()}";

            var tokenCacheList = await _userTokenCache.GetAsync(tokenKey);
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

            dto.TotalRecordCount = tokenList.Count;
            tokenList = tokenList.OrderBy(t => t.Symbol).ThenBy(t => t.ChainId).ToList();
            var defaultList = tokenList.Where(t => t.Symbol == CommonConstant.DefaultSymbol).ToList();

            var resultList = defaultList.Union(tokenList).Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount)
                .ToList();
            var symbols = resultList.Select(t => t.Symbol).ToList();
            dto.Data.AddRange(resultList);

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

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTokenAsync Error. {dto}", requestDto);
            return new GetTokenDto { Data = new List<Token>(), TotalRecordCount = 0 };
        }
    }

    private async Task CheckNeedAddTokenAsync(Guid userId, IndexerTokenInfos tokenInfos,
        List<UserTokenIndex> userTokens)
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
            var caAddressInfos = requestDto.CaAddressInfos;
            if (caAddressInfos == null)
            {
                caAddressInfos = requestDto.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var res = await _userAssetsProvider.GetUserNftCollectionInfoAsync(caAddressInfos,
                requestDto.SkipCount, requestDto.MaxResultCount);

            var dto = new GetNftCollectionsDto
            {
                Data = new List<NftCollection>(),
                TotalRecordCount = res?.CaHolderNFTCollectionBalanceInfo?.TotalRecordCount ?? 0
            };

            if (res?.CaHolderNFTCollectionBalanceInfo?.Data == null ||
                res.CaHolderNFTCollectionBalanceInfo.Data.Count == 0)
            {
                return dto;
            }

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

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTCollectionsAsync Error. {dto}", requestDto);
            return new GetNftCollectionsDto { Data = new List<NftCollection>(), TotalRecordCount = 0 };
        }
    }

    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        try
        {
            var caAddressInfos = requestDto.CaAddressInfos;
            if (caAddressInfos == null)
            {
                caAddressInfos = requestDto.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var res = await _userAssetsProvider.GetUserNftInfoAsync(caAddressInfos,
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

                dto.Data.Add(nftItem);
            }

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTItemsAsync Error. {dto}", requestDto);
            return new GetNftItemsDto { Data = new List<NftItem>(), TotalRecordCount = 0 };
        }
    }

    //Data with the same name needs to be deduplicated
    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(
        GetRecentTransactionUsersRequestDto requestDto)
    {
        try
        {
            var caAddressInfos = requestDto.CaAddressInfos;
            if (caAddressInfos == null)
            {
                caAddressInfos = requestDto.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var res = await _userAssetsProvider.GetRecentTransactionUsersAsync(caAddressInfos,
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

            var contactList = await _userContactProvider.BatchGetUserNameAsync(userCaAddresses, CurrentUser.GetId());
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
            await _userContactProvider.GetContactByUserNameAsync(user.Name, CurrentUser.GetId());
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
            var caAddressInfos = requestDto.CaAddressInfos;
            if (caAddressInfos == null)
            {
                caAddressInfos = requestDto.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var res = await _userAssetsProvider.SearchUserAssetsAsync(caAddressInfos,
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

                if (searchItem.TokenInfo != null)
                {
                    var price = decimal.Zero;
                    if (symbolPrices.ContainsKey(item.Symbol))
                    {
                        price = symbolPrices[item.Symbol];
                    }

                    var tokenInfo = ObjectMapper.Map<IndexerSearchTokenNft, TokenInfoDto>(searchItem);
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

                    item.NftInfo = ObjectMapper.Map<IndexerSearchTokenNft, NftInfoDto>(searchItem);

                    item.NftInfo.TokenId = searchItem.NftInfo.Symbol.Split("-").Last();

                    item.NftInfo.ImageUrl =
                        await _imageProcessProvider.GetResizeImageAsync(searchItem.NftInfo.ImageUrl, requestDto.Width,
                            requestDto.Height, ImageResizeType.Forest);
                }

                dto.Data.Add(item);
            }

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SearchUserAssetsAsync Error. {dto}", requestDto);
            return new SearchUserAssetsDto { Data = new List<UserAsset>(), TotalRecordCount = 0 };
        }
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

        return new TokenInfoDto
        {
            Balance = totalBalance.ToString()
        };
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
}