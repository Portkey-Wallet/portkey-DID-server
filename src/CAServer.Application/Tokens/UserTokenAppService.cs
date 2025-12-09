using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Etos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;
using Token = CAServer.UserAssets.Dtos.Token;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserTokenAppService : CAServerAppService, IUserTokenAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly TokenListOptions _tokenListOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly IDistributedCache<List<Token>> _userTokenCache;
    private readonly ITokenProvider _tokenProvider;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly ILogger<UserTokenAppService> _logger;

    public UserTokenAppService(
        IClusterClient clusterClient,
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IDistributedEventBus distributedEventBus,
        IDistributedCache<List<string>> distributedCache,
        ITokenProvider tokenProvider, IDistributedCache<List<Token>> userTokenCache,
        IAssetsLibraryProvider assetsLibraryProvider, IHttpContextAccessor httpContextAccessor,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions, ILogger<UserTokenAppService> logger)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _distributedCache = distributedCache;
        _tokenProvider = tokenProvider;
        _userTokenCache = userTokenCache;
        _assetsLibraryProvider = assetsLibraryProvider;
        _httpContextAccessor = httpContextAccessor;
        _nftToFtOptions = nftToFtOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
        _logger = logger;
    }

    [Authorize]
    public async Task<UserTokenDto> ChangeTokenDisplayAsync(bool isDisplay, string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var grainId))
            {
                var valueTuple = GetTokenInfoFromId(id);
                grainId = await AddTokenAsync(valueTuple.symbol, valueTuple.chainId);
            }

            //var isNeedDelete = !isDisplay && await IsNeedDeleteAsync(grainId);
            var grain = _clusterClient.GetGrain<IUserTokenGrain>(grainId);
            var userId = CurrentUser.GetId();
            var tokenResult = await grain.ChangeTokenDisplayAsync(userId, isDisplay, false);
            _logger.LogInformation("ChangeTokenDisplayAsync tokenResult = {0}",
                JsonConvert.SerializeObject(tokenResult));
            if (!tokenResult.Success)
            {
                throw new UserFriendlyException(tokenResult.Message);
            }

            await HandleTokenCacheAsync(userId, tokenResult.Data);
            await PublishAsync(tokenResult.Data, false);
            return ObjectMapper.Map<UserTokenGrainDto, UserTokenDto>(tokenResult.Data);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ChangeTokenDisplayAsync has error");
            throw e;
        }
    }

    private async Task HandleTokenCacheAsync(Guid userId, UserTokenGrainDto tokenDto)
    {
        var tokenKey = $"{CommonConstant.ResourceTokenKey}:{userId.ToString()}";
        var tokenList = await _userTokenCache.GetAsync(tokenKey);
        if (tokenList == null)
        {
            tokenList = new List<Token>();
        }

        var token = tokenList.FirstOrDefault(t =>
            t.Symbol == tokenDto.Token.Symbol && t.ChainId == tokenDto.Token.ChainId);

        if (tokenDto.IsDisplay && token != null)
        {
            tokenList.Remove(token);
        }

        if (!tokenDto.IsDisplay && token == null)
        {
            tokenList.Add(new Token()
            {
                ChainId = tokenDto.Token.ChainId,
                Symbol = tokenDto.Token.Symbol
            });
        }

        await _userTokenCache.SetAsync(tokenKey, tokenList, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
        });
    }

    public async Task<UserTokenDto> AddUserTokenAsync(Guid userId, AddUserTokenInput input)
    {
        Logger.LogInformation("start to add token.");
        var list = _tokenListOptions.UserToken.Select(async userTokenItem =>
            await InitialUserToken(userId, userTokenItem));
        await Task.WhenAll(list);
        return new UserTokenDto();
    }

    public async Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto)
    {
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        var userId = CurrentUser.GetId();
        var userTokens =
            await _tokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);

        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();
        // hide source tokens.
        userTokens.RemoveAll(t => sourceSymbols.Contains(t.Token.Symbol) && !t.IsDisplay);

        var tokens = ObjectMapper.Map<List<UserTokenIndex>, List<GetUserTokenDto>>(userTokens);
        foreach (var item in _tokenListOptions.UserToken)
        {
            var token = tokens.FirstOrDefault(t =>
                t.ChainId == item.Token.ChainId && t.Symbol == item.Token.Symbol);
            if (token != null)
            {
                continue;
            }

            tokens.Add(ObjectMapper.Map<UserTokenItem, GetUserTokenDto>(item));
        }

        if (!VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion))
        {
            tokens = tokens.Where(t => !_nftToFtOptions.NftToFtInfos.Keys.Contains(t.Symbol)).ToList();
        }

        if (!requestDto.Keyword.IsNullOrEmpty())
        {
            tokens = tokens.Where(t => t.Symbol.Trim().ToUpper().Contains(requestDto.Keyword.ToUpper())).ToList();
        }

        if (!requestDto.ChainIds.IsNullOrEmpty())
        {
            tokens = tokens.Where(t => requestDto.ChainIds.Contains(t.ChainId)).ToList();
        }

        foreach (var token in tokens)
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(token.Symbol);
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
                continue;
            }

            token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol, token.ImageUrl);
        }

        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        tokens = tokens.OrderBy(t => t.Symbol != CommonConstant.ELF)
            //.ThenBy(t => !t.IsDisplay)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => sourceSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenByDescending(t => t.ChainId)
            .ToList();

        return new PagedResultDto<GetUserTokenDto>(tokens.Count,
            tokens.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList());
    }

    private (string chainId, string symbol) GetTokenInfoFromId(string tokenId)
    {
        return (tokenId[..tokenId.IndexOf('-')], tokenId.Substring(tokenId.IndexOf('-') + 1));
    }

    private async Task<bool> IsNeedDeleteAsync(Guid tokenId)
    {
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(tokenId);
        var resultDto = await grain.GetUserToken();
        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        var symbol = resultDto.Data.Token.Symbol;
        var symbols = await _distributedCache.GetAsync(CommonConstant.ResourceTokenKey);
        return !symbols.Contains(symbol);
    }

    private async Task<Guid> AddTokenAsync(string symbol, string chainId)
    {
        if (_nftToFtOptions.NftToFtInfos.Keys.Contains(symbol))
        {
            var userTokenInfo =
                _tokenListOptions.UserToken.FirstOrDefault(t => t.Token.Symbol == symbol && t.Token.ChainId == chainId);
            return await InitialUserToken(CurrentUser.GetId(), userTokenInfo);
        }

        var indexerTokens = await _tokenProvider.GetTokenInfosAsync(chainId, symbol, string.Empty, 0, 1);
        var tokenInfo = indexerTokens?.TokenInfo?.FirstOrDefault();
        if (tokenInfo == null)
        {
            throw new UserFriendlyException("Token not found.");
        }

        return await InitialUserToken(CurrentUser.GetId(),
            ObjectMapper.Map<IndexerToken, UserTokenItem>(tokenInfo));
    }

    private async Task<Guid> InitialUserToken(Guid userId, UserTokenItem userTokenItem)
    {
        var userTokenSymbol = _clusterClient.GetGrain<IUserTokenSymbolGrain>(
            GrainIdHelper.GenerateGrainId(userId.ToString("N"), userTokenItem.Token.ChainId,
                userTokenItem.Token.Symbol));
        var ifExist =
            await userTokenSymbol.IsUserTokenSymbolExistAsync(userTokenItem.Token.ChainId,
                userTokenItem.Token.Symbol);
        if (ifExist)
        {
            throw new UserFriendlyException("User token already existed.");
        }

        var userTokenId = GuidGenerator.Create();
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(userTokenId);
        var tokenItem = ObjectMapper.Map<UserTokenItem, UserTokenGrainDto>(userTokenItem);
        var addTokenResult = await grain.AddUserTokenAsync(userId, tokenItem);
        if (!addTokenResult.Success)
        {
            throw new UserFriendlyException(addTokenResult.Message);
        }

        var toPublish = ObjectMapper.Map<UserTokenGrainDto, UserTokenEto>(addTokenResult.Data);
        Logger.LogInformation(
            $"publish add user token event：{toPublish.UserId}-{toPublish.Token.ChainId}-{toPublish.Token.Symbol}");
        await _distributedEventBus.PublishAsync(toPublish);

        return userTokenId;
    }

    private async Task PublishAsync(UserTokenGrainDto dto, bool isDelete)
    {
        if (isDelete)
        {
            await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserTokenGrainDto, UserTokenDeleteEto>(dto));
            return;
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserTokenGrainDto, UserTokenEto>(dto));
    }
}