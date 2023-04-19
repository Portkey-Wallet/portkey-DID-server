using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;
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

    public UserAssetsAppService(
        ILogger<UserAssetsAppService> logger, IUserAssetsProvider userAssetsProvider, ITokenAppService tokenAppService,
        IUserContactProvider userContactProvider, IOptions<TokenInfoOptions> tokenInfoOptions)
    {
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
        _userContactProvider = userContactProvider;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _tokenAppService = tokenAppService;
    }

    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetUserTokenInfoAsync(requestDto.CaAddresses, "",
                0, requestDto.SkipCount + requestDto.MaxResultCount);

            var chainInfos = await _userAssetsProvider.GetUserChainIdsAsync(requestDto.CaAddresses);
            var chainIds = chainInfos.CaHolderManagerInfo.Select(c => c.ChainId).Distinct().ToList();

            var dto = new GetTokenDto
            {
                Data = new List<Token>(),
                TotalRecordCount = 0
            };

            var userDefaultTokenSymbols = await _userAssetsProvider.GetUserDefaultTokenSymbolAsync(CurrentUser.GetId());

            var userTokenSymbols = new List<UserTokenIndex>();

            userTokenSymbols.AddRange(userDefaultTokenSymbols);
            userTokenSymbols.AddRange(await _userAssetsProvider.GetUserIsDisplayTokenSymbolAsync(CurrentUser.GetId()));

            if (userTokenSymbols.IsNullOrEmpty())
            {
                _logger.LogError("get no result from current user {id}", CurrentUser.GetId());
                return dto;
            }

            var list = new List<Token>();

            foreach (var symbol in userTokenSymbols)
            {
                if (!chainIds.Contains(symbol.Token.ChainId))
                {
                    continue;
                }

                var tokenInfo = res.CaHolderTokenBalanceInfo.Data.FirstOrDefault(t =>
                    t.TokenInfo.Symbol == symbol.Token.Symbol && t.ChainId == symbol.Token.ChainId);
                if (tokenInfo == null)
                {
                    var data = await _userAssetsProvider.GetUserTokenInfoAsync(requestDto.CaAddresses,
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
                    res.CaHolderTokenBalanceInfo.Data.Remove(tokenInfo);
                }

                var token = ObjectMapper.Map<IndexerTokenInfo, Token>(tokenInfo);

                if (_tokenInfoOptions.TokenInfos.ContainsKey(token.Symbol))
                {
                    token.ImageUrl = _tokenInfoOptions.TokenInfos[token.Symbol].ImageUrl;
                }

                list.Add(token);
            }

            if (!res.CaHolderTokenBalanceInfo.Data.IsNullOrEmpty())
            {
                var userNotDisplayTokenAsync =
                    await _userAssetsProvider.GetUserNotDisplayTokenAsync(CurrentUser.GetId());

                while (list.Count < requestDto.MaxResultCount + requestDto.SkipCount)
                {
                    var userAsset = res.CaHolderTokenBalanceInfo.Data.FirstOrDefault();
                    if (userAsset == null)
                    {
                        break;
                    }

                    if (!userNotDisplayTokenAsync.Contains((userAsset.TokenInfo.Symbol, userAsset.ChainId)))
                    {
                        list.Add(ObjectMapper.Map<IndexerTokenInfo, Token>(userAsset));
                    }

                    res.CaHolderTokenBalanceInfo.Data.Remove(userAsset);
                }
            }

            dto.TotalRecordCount = list.Count;

            var resultList = new List<Token>();

            list.Sort((t1, t2) => t1.Symbol != t2.Symbol
                ? string.Compare(t1.Symbol, t2.Symbol, StringComparison.Ordinal)
                : string.Compare(t1.ChainId, t2.ChainId, StringComparison.Ordinal));

            resultList.AddRange(list.Where(t => userDefaultTokenSymbols.Select(s => s.Token.Symbol).Contains(t.Symbol))
                .ToList());
            resultList.AddRange(list.Where(t => !userDefaultTokenSymbols.Select(s => s.Token.Symbol).Contains(t.Symbol))
                .ToList());

            resultList = resultList.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
            var symbols = resultList.Select(t => t.Symbol).ToList();

            dto.Data.AddRange(resultList);

            var priceDict = await GetSymbolPrice(symbols);
            foreach (var token in dto.Data)
            {
                if (!priceDict.ContainsKey(token.Symbol))
                {
                    continue;
                }

                var balanceInUsd = priceDict[token.Symbol] * long.Parse(token.Balance);
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

    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetUserNftCollectionInfoAsync(requestDto.CaAddresses,
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
                dto.Data.Add(ObjectMapper.Map<IndexerNftCollectionInfo, NftCollection>(nftCollectionInfo));
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
            var res = await _userAssetsProvider.GetUserNftInfoAsync(requestDto.CaAddresses,
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

    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(
        GetRecentTransactionUsersRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.GetRecentTransactionUsersAsync(requestDto.CaAddresses,
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

            var userCaAddresses = new List<string>();

            foreach (var info in res.CaHolderTransactionAddressInfo.Data)
            {
                dto.Data.Add(ObjectMapper.Map<CAHolderTransactionAddress, RecentTransactionUser>(info));

                if (!userCaAddresses.Contains(info.Address))
                {
                    userCaAddresses.Add(info.Address);
                }
            }

            var contactList = await _userContactProvider.GetUserNameBatch(userCaAddresses, CurrentUser.GetId());
            var nameList = contactList.Select(t => t.Item1.Address).ToList();
            foreach (var user in dto.Data.Where(user => nameList.Contains(user.Address)))
            {
                var contact =
                    contactList.FirstOrDefault(t => t.Item1.Address == user.Address && t.Item1.ChainId == user.ChainId);

                user.Name = contact?.Item2;
            }

            return dto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRecentTransactionUsersAsync Error. {dto}", requestDto);
            return new GetRecentTransactionUsersDto { Data = new List<RecentTransactionUser>(), TotalRecordCount = 0 };
        }
    }

    public async Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        try
        {
            var res = await _userAssetsProvider.SearchUserAssetsAsync(requestDto.CaAddresses,
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
                    tokenInfo.BalanceInUsd = tokenInfo.BalanceInUsd = (searchItem.Balance * price).ToString();
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