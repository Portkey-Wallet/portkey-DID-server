using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Validation;
using NftProtocol = CAServer.UserAssets.Dtos.NftProtocol;
using Token = CAServer.UserAssets.Dtos.Token;

namespace CAServer.UserAssets;

[RemoteService(false)]
[DisableAuditing]
public class UserAssetsAppService : CAServerAppService, IUserAssetsAppService, IValidationEnabled
{
    private readonly ILogger<UserAssetsAppService> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IUserAssetsProvider _userAssetsProvider;

    public UserAssetsAppService(
        ILogger<UserAssetsAppService> logger, IUserAssetsProvider userAssetsProvider)
    {
        _logger = logger;
        _userAssetsProvider = userAssetsProvider;
    }

    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        var res = await _userAssetsProvider.GetTokenAsync(requestDto.CaAddresses, requestDto.SkipCount, requestDto.MaxResultCount);
        if (res?.CaHolderTokenBalanceInfo == null)
        {
            _logger.LogError("get none of result from token grain, address={caAddresses}", requestDto.CaAddresses);
            return null;
        }

        var defaultTokenSymbol = await GetUserDefaultToken();
        var symbols = new List<string>();
        var dto = new GetTokenDto() { Tokens = new List<Token>() };
        foreach (var tokenBalance in res.CaHolderTokenBalanceInfo.Where(tokenBalance => tokenBalance.Balance != 0 || tokenBalance.IndexerTokenInfo.Symbol == defaultTokenSymbol))
        {
            dto.Tokens.Add(ObjectMapper.Map<TokenBalance, Token>(tokenBalance));
            symbols.Add(tokenBalance.IndexerTokenInfo.Symbol);
        }

        var priceDict = await GetSymbolPrice(symbols);
        foreach (var token in dto.Tokens)
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

    private async Task<string> GetUserDefaultToken()
    {
        return ""; //todo get default token
    }

    public async Task<GetNFTProtocolsDto> GetNFTProtocolsAsync(GetNFTProtocolsRequestDto requestDto)
    {
        var res = await _userAssetsProvider.GetNFTProtocolsAsync(requestDto.CaAddresses, requestDto.SkipCount, requestDto.MaxResultCount);
        if (res?.userNFTProtocolInfo == null || res.userNFTProtocolInfo.Count == 0)
        {
            return new GetNFTProtocolsDto();
        }

        var dto = new GetNFTProtocolsDto() { Data = new List<NftProtocol>(res.userNFTProtocolInfo.Count) };
        foreach (var protocol in res.userNFTProtocolInfo.Where(protocol => protocol.TokenIds != null && protocol.TokenIds.Count != 0 && protocol.NftProtocolInfo != null))
        {
            dto.Data.Add(ObjectMapper.Map<Provider.NftProtocol, NftProtocol>(protocol));
        }

        return dto;
    }

    public async Task<GetNFTItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        const int getItemsSkipCount = 0;
        const int getItemsMaxResultCount = 2;
        var res = await _userAssetsProvider.GetNftInfosAsync(requestDto.CaAddresses, requestDto.Symbol, getItemsSkipCount, getItemsMaxResultCount);
        if (res?.UserNFTInfo == null || res.UserNFTInfo.Count == 0)
        {
            return new GetNFTItemsDto();
        }

        var dto = new GetNFTItemsDto() { Data = new List<NftItem>(res.UserNFTInfo.Count) };
        foreach (var info in res.UserNFTInfo.Where(info => info.NftInfo != null))
        {
            dto.Data.Add(ObjectMapper.Map<UserNftInfo, NftItem>(info));
        }

        return dto;
    }

    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(GetRecentTransactionUsersRequestDto requestDto)
    {
        var res = await _userAssetsProvider.GetRecentTransactionUsersAsync(requestDto.CaAddresses, requestDto.SkipCount, requestDto.MaxResultCount);
        if (res?.CaHolderTransactionAddressInfo == null || res.CaHolderTransactionAddressInfo.Count == 0)
        {
            return new GetRecentTransactionUsersDto();
        }

        var dto = new GetRecentTransactionUsersDto() { Data = new List<RecentTransactionUser>(res.CaHolderTransactionAddressInfo.Count) };
        var userCaAddresses = new List<ValueTuple<string, string>>();
        foreach (var info in res.CaHolderTransactionAddressInfo)
        {
            dto.Data.Add(ObjectMapper.Map<CAHolderTransactionAddress, RecentTransactionUser>(info));
            userCaAddresses.Add(new ValueTuple<string, string>(info.CaAddress, info.ChainId));
        }

        //get all user's managerAddress
        var managerDict = await GetUserNameBatch(userCaAddresses);
        foreach (var user in dto.Data)
        {
            var userAddress = new ValueTuple<string, string>(user.CaAddress, user.ChainId);
            if (!managerDict.ContainsKey(userAddress))
            {
                continue;
            }

            user.Name = managerDict[userAddress];
        }

        return dto;
    }

    public async Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        var res = await _userAssetsProvider.SearchUserAssetsAsync(requestDto.CaAddresses, requestDto.KeyWord, requestDto.SkipCount, requestDto.MaxResultCount);
        if (res?.caHolderSearchTokenNFT == null)
        {
            return new SearchUserAssetsDto();
        }

        var dto = new SearchUserAssetsDto() { Data = new List<UserAsset>(res.caHolderSearchTokenNFT.Count) };
        var symbols = (from searchItem in res.caHolderSearchTokenNFT where searchItem.IndexerTokenInfo != null select searchItem.IndexerTokenInfo.Symbol).ToList();
        var symbolPrices = await GetSymbolPrice(symbols);
        foreach (var searchItem in res.caHolderSearchTokenNFT)
        {
            var item = ObjectMapper.Map<IndexerUserAsset, UserAsset>(searchItem);
            if (searchItem.IndexerTokenInfo != null)
            {
                decimal price = 0;
                if (symbolPrices.ContainsKey(item.Symbol))
                {
                    price = symbolPrices[item.Symbol];
                }

                var tokenInfo = ObjectMapper.Map<IndexerUserAsset, TokenInfo>(searchItem);
                tokenInfo.BalanceInUsd = (searchItem.Balance * price).ToString();
                item.TokenInfo = tokenInfo;
            }
            else if (searchItem.NftInfo != null)
            {
                item.NftInfo = ObjectMapper.Map<IndexerUserAsset, NftInfo>(searchItem);
            }

            dto.Data.Add(item);
        }

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
                dict[price.Symbol] = price.PriceInUsd;
            }

            return dict;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get symbols price failed, symbol={symbols}", symbols);
            throw e;
        }
    }

    private async Task<Dictionary<ValueTuple<string, string>, string>> GetUserNameBatch(List<ValueTuple<string, string>> usersAddresses)
    {
        return new Dictionary<ValueTuple<string, string>, string>();
    }
}