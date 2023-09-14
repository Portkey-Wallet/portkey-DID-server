using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MongoDB.Driver.Linq;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
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
    private readonly IContactProvider _contactProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IGuardianProvider _guardianProvider;
    private const int MaxResultCount = 10;
    private readonly SyncOriginChainIdOptions _syncOriginChainIdOptions;
    private readonly IDistributedEventBus _distributedEventBus;

    public UserAssetsAppService(
        ILogger<UserAssetsAppService> logger, IUserAssetsProvider userAssetsProvider, ITokenAppService tokenAppService,
        IUserContactProvider userContactProvider, IOptions<TokenInfoOptions> tokenInfoOptions,
        IImageProcessProvider imageProcessProvider, IOptions<ChainOptions> chainOptions,IOptions<SyncOriginChainIdOptions> syncOriginChainIdOptions,
        IContractProvider contractProvider, IContactProvider contactProvider, IClusterClient clusterClient,
        IGuardianProvider guardianProvider, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
        _userContactProvider = userContactProvider;
        _tokenInfoOptions = tokenInfoOptions.Value;
        _tokenAppService = tokenAppService;
        _imageProcessProvider = imageProcessProvider;
        _contractProvider = contractProvider;
        _contactProvider = contactProvider;
        _chainOptions = chainOptions.Value;
        _clusterClient = clusterClient;
        _guardianProvider = guardianProvider;
        _distributedEventBus = distributedEventBus;
        _syncOriginChainIdOptions = syncOriginChainIdOptions.Value;
    }

    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        try
        {
            if (await NeedSyncStatusAsync(CurrentUser.GetId()))
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

            var res = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
                0, requestDto.SkipCount + requestDto.MaxResultCount);

            res.CaHolderTokenBalanceInfo.Data =
                res.CaHolderTokenBalanceInfo.Data.Where(t => t.TokenInfo != null).ToList();

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
                if (nftCollectionInfo == null || nftCollectionInfo.NftCollectionInfo == null)
                {
                    dto.Data.Add(nftCollection);
                }
                else
                {
                    nftCollection.ImageUrl = await _imageProcessProvider.GetResizeImageAsync(
                        nftCollectionInfo.NftCollectionInfo.ImageUrl, requestDto.Width, requestDto.Height,
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
                nftItem.ImageUrl =
                    await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl, requestDto.Width, requestDto.Height,
                        ImageResizeType.Forest);
                nftItem.ImageLargeUrl = await _imageProcessProvider.GetResizeImageAsync(nftInfo.NftInfo.ImageUrl,
                    (int)ImageResizeWidthType.IMAGE_WIDTH_TYPE_ONE, (int)ImageResizeHeightType.IMAGE_HEIGHT_TYPE_AUTO,ImageResizeType.Forest);

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

    public async Task CheckOriginChainIdStatusAsync(UserLoginEto userLoginEto)
    {
        if (!await NeedSyncStatusAsync(userLoginEto.UserId))
        {
            return;
        }
        
        var originChainId = "";
        var syncChainId = "";
        var guardians = await _guardianProvider.GetGuardiansAsync("", userLoginEto.CaHash);
        if (guardians == null || !guardians.CaHolderInfo.Any())
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,guardians is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        originChainId = guardians.CaHolderInfo?.FirstOrDefault()?.OriginChainId;
        if (string.IsNullOrWhiteSpace(originChainId))
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,originChainId is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        syncChainId = _chainOptions.ChainInfos.Where(kvp => kvp.Key != originChainId).Select(kvp => kvp.Key)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(syncChainId))
        {
            _logger.LogInformation("CheckOriginChainIdStatusAsync fail,syncChainId is null or empty,userId {uid}",
                userLoginEto.UserId);
            return;
        }

        await UpdateOriginChainIdAsync(originChainId, syncChainId, userLoginEto);
    }

    public async Task UpdateOriginChainIdAsync(string originChainId, string syncChainId, UserLoginEto userLoginEto)
    {
        var validateOriginChainIdGrain = _clusterClient.GetGrain<IValidateOriginChainIdGrain>(userLoginEto.UserId);
        try
        {
            var needValidate = await validateOriginChainIdGrain.NeedValidateAsync();
            _logger.LogInformation("UpdateOriginChainIdAsync,needValidate {needValidate}", needValidate);
            
            if (!needValidate.Data)
            {
                return;
            }
            
            var holderInfoOutput =
                await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(userLoginEto.CaHash),
                    null, originChainId);

            var syncHolderInfoOutput =
                await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(userLoginEto.CaHash),
                    null, syncChainId);
            
            if (holderInfoOutput.CreateChainId > 0 && syncHolderInfoOutput.CreateChainId > 0)
            {
                await validateOriginChainIdGrain.SetStatusSuccessAsync();
                _logger.LogInformation(
                    "UpdateOriginChainIdAsync success,chainId {chainId},userId {uid}", originChainId,
                    userLoginEto.UserId);
                return;
            }

            _logger.LogInformation(
                "UpdateOriginChainIdAsync success,originChainId {originChainId}:{holderInfoOutput.CreateChainId}, syncChainId:{syncChainId}:{syncHolderInfoOutput.CreateChainId},userId {uid}",
                originChainId, holderInfoOutput.CreateChainId, syncChainId, syncHolderInfoOutput.CreateChainId,
                userLoginEto.UserId);

            holderInfoOutput.CreateChainId = ChainHelper.ConvertBase58ToChainId(originChainId);
            
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var transactionDto =
                await grain.ValidateTransactionAsync(originChainId, holderInfoOutput, null);

            await validateOriginChainIdGrain.SetInfoAsync(transactionDto.TransactionResultDto.TransactionId,
                originChainId);

            if (transactionDto.TransactionResultDto.Status == TransactionState.Mined)
            {
                await validateOriginChainIdGrain.SetStatusSuccessAsync();
                _logger.LogInformation(
                    "UpdateOriginChainIdAsync success,chainId {chainId},transactionId {transactionId},transactionStatus {transactionStatus}",
                    originChainId,
                    transactionDto.TransactionResultDto.TransactionId,
                    transactionDto.TransactionResultDto.Status);
                return;
            }

            if (transactionDto.TransactionResultDto.Status == TransactionState.NodeValidationFailed ||
                transactionDto.TransactionResultDto.Status == TransactionState.Failed)
            {
                await validateOriginChainIdGrain.SetStatusFailAsync();
                _logger.LogInformation(
                    "UpdateOriginChainIdAsync fail status {status} ,chainId {chainId},transactionId {transactionId},transactionStatus {transactionStatus}",
                    transactionDto.TransactionResultDto.Status, originChainId,
                    transactionDto.TransactionResultDto.TransactionId,
                    transactionDto.TransactionResultDto.Status);
                return;
            }

            _logger.LogInformation(
                "UpdateOriginChainIdAsync success status {status},chainId {chainId},transactionId {transactionId},transactionStatus {transactionStatus}",
                transactionDto.TransactionResultDto.Status, originChainId,
                transactionDto.TransactionResultDto.TransactionId,
                transactionDto.TransactionResultDto.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateOriginChainIdAsync fail,chainId {chainId},userId {uid}", originChainId,
                userLoginEto.UserId);
            await validateOriginChainIdGrain.SetStatusFailAsync();
        }
    }
    
    public async Task<bool> NeedSyncStatusAsync(Guid userId)
    {
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(userId);
        if (caHolderIndex == null || caHolderIndex.IsDeleted)
        {
            _logger.LogInformation("UpdateOriginChainIdAsync caHolderIndex is null or deleted,userId {uid}", userId);
            return false;
        }

        _logger.LogInformation(
            "UpdateOriginChainIdAsync caHolderIndex.CreateTime:{caHolderIndex.CreateTime},checkTime:{time}",
            (TimeHelper.GetTimeStampFromDateTime(caHolderIndex.CreateTime),
                _syncOriginChainIdOptions.CheckUserRegistrationTimestamp));
        
        if (TimeHelper.GetTimeStampFromDateTime(caHolderIndex.CreateTime) > _syncOriginChainIdOptions.CheckUserRegistrationTimestamp)
        {
            return false;
        }
        
        return true;
    }
}