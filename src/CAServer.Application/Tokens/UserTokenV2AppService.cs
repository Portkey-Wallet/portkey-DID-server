using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.Users;

namespace CAServer.Tokens;

[DisableAuditing, RemoteService(IsEnabled = false)]
public class UserTokenV2AppService : CAServerAppService, IUserTokenV2AppService
{
    private readonly IUserTokenAppService _tokenAppService;
    private readonly ITokenProvider _tokenProvider;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly TokenListOptions _tokenListOptions;
    private readonly ITokenNftAppService _tokenNftAppService;
    private readonly ChainOptions _chainOptions;
    private readonly IDistributedCache<IndexerToken> _tokenInfoCache;
    private readonly AddTokenOptions _addTokenOptions;
    private readonly ILogger<UserTokenV2AppService> _logger;
    public UserTokenV2AppService(IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IUserTokenAppService tokenAppService, ITokenProvider tokenProvider,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions, IAssetsLibraryProvider assetsLibraryProvider,
        ITokenNftAppService tokenNftAppService, IOptions<ChainOptions> chainOptions,
        IDistributedCache<IndexerToken> tokenInfoCache,
        IOptionsSnapshot<AddTokenOptions> addTokenOptions,
        ILogger<UserTokenV2AppService> logger)
    {
        _tokenAppService = tokenAppService;
        _tokenProvider = tokenProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
        _tokenNftAppService = tokenNftAppService;
        _tokenInfoCache = tokenInfoCache;
        _nftToFtOptions = nftToFtOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
        _chainOptions = chainOptions.Value;
        _addTokenOptions = addTokenOptions.Value;
        this._logger = logger;
    }

    public async Task ChangeTokenDisplayAsync(ChangeTokenDisplayDto requestDto)
    {
        foreach (var id in requestDto.Ids)
        {
            await _tokenAppService.ChangeTokenDisplayAsync(requestDto.IsDisplay, id);
        }
    }

    public async Task<CaPageResultDto<GetUserTokenV2Dto>> GetTokensAsync(GetTokenInfosV2RequestDto requestDto)
    {
        var userId = CurrentUser.GetId();
        var userTokens =
            await _tokenProvider.GetUserTokenInfoListAsync(userId, string.Empty, string.Empty);
        
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

        if (!requestDto.Keyword.IsNullOrEmpty())
        {
            tokens = tokens.Where(t => t.Symbol.Trim().ToUpper().Contains(requestDto.Keyword.ToUpper())).ToList();
        }

        var chainIds = _chainOptions.ChainInfos.Keys.ToList();
        tokens = tokens.Where(t => chainIds.Contains(t.ChainId)).ToList();

        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();
        tokens = tokens.OrderBy(t => t.Symbol != CommonConstant.ELF)
            .ThenBy(t => !t.IsDisplay)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => sourceSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenBy(t => t.ChainId)
            .ToList();

        var data = await GetTokensAsync(tokens);
        return new CaPageResultDto<GetUserTokenV2Dto>(data.Count,
            data.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList());
    }

    public async Task<CaPageResultDto<GetTokenListV2Dto>> GetTokenListAsync(GetTokenListV2RequestDto requestDto)
    {
        var tokens = await _tokenNftAppService.GetTokenListAsync(new GetTokenListRequestDto
        {
            ChainIds = _chainOptions.ChainInfos.Keys.ToList(),
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
            SkipCount = TokensConstants.SkipCountDefault,
            Symbol = requestDto.Symbol
        });

        var data = GetTokens(tokens);
        return new CaPageResultDto<GetTokenListV2Dto>(data.Count,
            data.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList());
    }

    private async Task<List<GetUserTokenV2Dto>> GetTokensAsync(List<GetUserTokenDto> tokens)
    {
        var result = new List<GetUserTokenV2Dto>();
        if (tokens.IsNullOrEmpty()) return result;
        var sourceSymbols = _tokenListOptions.SourceToken.Select(t => t.Token.Symbol).Distinct().ToList();

        foreach (var group in tokens.GroupBy(t => t.Symbol))
        {
            var tokenItems = group.ToList();
            if (sourceSymbols.Contains(group.Key) && tokenItems.All(t => !t.IsDisplay))
            {
                continue;
            }
            
            var tokenItem = group.First();
            var tokenList = await CheckTokenAsync(group.ToList());
            SetTokenInfo(tokenList);
            var userToken = new GetUserTokenV2Dto
            {
                Symbol = group.Key,
                ImageUrl = tokenItem.ImageUrl,
                Label = tokenItem.Label,
                IsDefault = tokenItem.IsDefault,
                Tokens = tokenList
            };

            var displayList = userToken.Tokens.Select(t => t.IsDisplay);
            userToken.DisplayStatus = displayList.All(t => t == true) ? TokenDisplayStatus.All.ToString() :
                displayList.All(t => t == false) ? TokenDisplayStatus.None.ToString() :
                TokenDisplayStatus.Partial.ToString();
            result.Add(userToken);
        }

        return result;
    }

    private List<GetTokenListV2Dto> GetTokens(List<GetTokenListDto> tokens)
    {
        var result = new List<GetTokenListV2Dto>();
        if (tokens.IsNullOrEmpty()) return result;

        foreach (var group in tokens.GroupBy(t => t.Symbol))
        {
            var tokenItem = group.First();
            var userToken = new GetTokenListV2Dto
            {
                Symbol = group.Key,
                ImageUrl = tokenItem.ImageUrl,
                Label = tokenItem.Label,
                IsDefault = tokenItem.IsDefault,
                Tokens = group.ToList()
            };

            var displayList = userToken.Tokens.Select(t => t.IsDisplay);
            userToken.DisplayStatus = displayList.All(t => t == true) ? TokenDisplayStatus.All.ToString() :
                displayList.All(t => t == false) ? TokenDisplayStatus.None.ToString() :
                TokenDisplayStatus.Partial.ToString();
            result.Add(userToken);
        }

        return result;
    }

    private void SetTokenInfo(List<GetUserTokenDto> tokens)
    {
        foreach (var token in tokens)
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(token.Symbol);
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
                return;
            }

            token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol);
        }
    }

    private async Task<List<GetUserTokenDto>> CheckTokenAsync(List<GetUserTokenDto> userTokens)
    {
        if (userTokens.Count == _chainOptions.ChainInfos.Keys.Count)
        {
            return userTokens;
        }

        var userToken = userTokens.First();
        var chainId = _chainOptions.ChainInfos.Keys.First(t => t != userToken.ChainId);

        var tokenInfoCache = await _tokenInfoCache.GetAsync(GetTokenCacheKey(chainId, userToken.Symbol));
        if (tokenInfoCache != null && !tokenInfoCache.Symbol.IsNullOrEmpty())
        {
            Logger.LogInformation("get from cache, chainId:{chainId}, symbol:{symbol}", chainId, userToken.Symbol);
            userTokens.Add(ObjectMapper.Map<IndexerToken, GetUserTokenDto>(tokenInfoCache));
            return userTokens.OrderBy(t => t.ChainId).ToList();
        }

        var tokenInfos = await _tokenProvider.GetTokenInfosAsync(chainId, userToken.Symbol, string.Empty);
        var tokenInfo = tokenInfos?.TokenInfo?.FirstOrDefault();
        if (tokenInfo == null)
        {
            await _tokenInfoCache.SetAsync(GetTokenCacheKey(chainId, userToken.Symbol), new IndexerToken(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.UtcNow.AddMinutes(_addTokenOptions.CacheExpirationTime)
                });
        }

        Logger.LogInformation("get from indexer, chainId:{chainId}, symbol:{symbol}", chainId, userToken.Symbol);
        await _tokenInfoCache.SetAsync(GetTokenCacheKey(chainId, userToken.Symbol), tokenInfo,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            });

        if (null != tokenInfo)
        {
            userTokens.Add(ObjectMapper.Map<IndexerToken, GetUserTokenDto>(tokenInfo));
        }
        
        return userTokens.OrderBy(t => t.ChainId).ToList();
    }

    private string GetTokenCacheKey(string chainId, string symbol)
    {
        return string.Join(':', CommonConstant.TokenInfoCachePrefix, chainId, symbol);
    }
}