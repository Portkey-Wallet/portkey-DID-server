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
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
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

    public UserTokenV2AppService(IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IUserTokenAppService tokenAppService, ITokenProvider tokenProvider,
        IOptionsSnapshot<NftToFtOptions> nftToFtOptions, IAssetsLibraryProvider assetsLibraryProvider,
        ITokenNftAppService tokenNftAppService, IOptions<ChainOptions> chainOptions)
    {
        _tokenAppService = tokenAppService;
        _tokenProvider = tokenProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
        _tokenNftAppService = tokenNftAppService;
        _nftToFtOptions = nftToFtOptions.Value;
        _tokenListOptions = tokenListOptions.Value;
        _chainOptions = chainOptions.Value;
    }

    public async Task ChangeTokenDisplayAsync(ChangeTokenDisplayDto requestDto)
    {
        var tasks = requestDto.Ids.Select(id => _tokenAppService.ChangeTokenDisplayAsync(requestDto.IsDisplay, id))
            .Cast<Task>().ToList();
        await Task.WhenAll(tasks);
    }

    public async Task<CaPageResultDto<GetUserTokenV2Dto>> GetTokensAsync(GetTokenInfosV2RequestDto requestDto)
    {
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

        tokens = tokens.Where(t => !_nftToFtOptions.NftToFtInfos.Keys.Contains(t.Symbol)).ToList();

        if (!requestDto.Keyword.IsNullOrEmpty())
        {
            tokens = tokens.Where(t => t.Symbol.Trim().ToUpper().Contains(requestDto.Keyword.ToUpper())).ToList();
        }

        var chainIds = _chainOptions.ChainInfos.Keys.ToList();
        tokens = tokens.Where(t => chainIds.Contains(t.ChainId)).ToList();

        foreach (var token in tokens)
        {
            var nftToFtInfo = _nftToFtOptions.NftToFtInfos.GetOrDefault(token.Symbol);
            if (nftToFtInfo != null)
            {
                token.Label = nftToFtInfo.Label;
                token.ImageUrl = nftToFtInfo.ImageUrl;
                continue;
            }

            token.ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(token.Symbol);
        }

        var defaultSymbols = _tokenListOptions.UserToken.Select(t => t.Token.Symbol).Distinct().ToList();
        tokens = tokens.OrderBy(t => t.Symbol != CommonConstant.ELF)
            .ThenBy(t => !t.IsDisplay)
            .ThenBy(t => !defaultSymbols.Contains(t.Symbol))
            .ThenBy(t => sourceSymbols.Contains(t.Symbol))
            .ThenBy(t => Array.IndexOf(defaultSymbols.ToArray(), t.Symbol))
            .ThenBy(t => t.Symbol)
            .ThenBy(t => t.ChainId)
            .ToList();

        var data = GetTokens(tokens);

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

    private List<GetUserTokenV2Dto> GetTokens(List<GetUserTokenDto> tokens)
    {
        var result = new List<GetUserTokenV2Dto>();
        if (tokens.IsNullOrEmpty()) return result;

        foreach (var group in tokens.GroupBy(t => t.Symbol))
        {
            var tokenItem = group.First();
            var userToken = new GetUserTokenV2Dto
            {
                Symbol = group.Key,
                ImageUrl = tokenItem.ImageUrl,
                Label = tokenItem.Label,
                IsDefault = tokenItem.IsDefault,
                Tokens = group.ToList()
            };

            var displayList = userToken.Tokens.Select(t => t.IsDisplay);
            userToken.DisplayStatus = displayList.All(t => t == true) ? TokenDisplayStatus.All :
                displayList.All(t => t == false) ? TokenDisplayStatus.None : TokenDisplayStatus.Partial;
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
            userToken.DisplayStatus = displayList.All(t => t == true) ? TokenDisplayStatus.All :
                displayList.All(t => t == false) ? TokenDisplayStatus.None : TokenDisplayStatus.Partial;
            result.Add(userToken);
        }

        return result;
    }
}