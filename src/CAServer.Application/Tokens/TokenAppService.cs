using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Options;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.Users;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TokenAppService : CAServerAppService, ITokenAppService
{
    private readonly ContractAddressOptions _contractAddressOptions;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDistributedCache<TokenExchange> _latestExchange;
    private readonly Dictionary<string, IExchangeProvider> _exchangeProviders;
    private readonly ITokenCacheProvider _tokenCacheProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly IOptionsMonitor<TokenSpenderOptions> _tokenSpenderOptions;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<TokenAppService> _logger;

    public TokenAppService(IOptions<ContractAddressOptions> contractAddressesOptions,
        ITokenProvider tokenProvider, IEnumerable<IExchangeProvider> exchangeProviders,
        IDistributedCache<TokenExchange> latestExchange,
        IDistributedCache<TokenExchange> historyExchange,
        ITokenCacheProvider tokenCacheProvider,
        ITokenPriceService tokenPriceService,
        IOptionsMonitor<TokenSpenderOptions> tokenSpenderOptions,
        IAssetsLibraryProvider assetsLibraryProvider,
        IContractProvider contractProvider,
        ILogger<TokenAppService> logger)
    {
        _tokenProvider = tokenProvider;
        _latestExchange = latestExchange;
        _tokenPriceService = tokenPriceService;
        _contractAddressOptions = contractAddressesOptions.Value;
        _exchangeProviders = exchangeProviders.ToDictionary(p => p.Name().ToString(), p => p);
        _tokenCacheProvider = tokenCacheProvider;
        _tokenSpenderOptions = tokenSpenderOptions;
        _assetsLibraryProvider = assetsLibraryProvider;
        _contractProvider = contractProvider;
        _logger = logger;
    }

    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
    {
        var result = new List<TokenPriceDataDto>();
        if (symbols.Count == 0)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }

        try
        {
            var symbolList = symbols.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
            foreach (var symbol in symbolList)
            {
                var priceResult = await _tokenPriceService.GetCurrentPriceAsync(symbol);
                result.Add(priceResult);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Get price failed. Error message:{ex.Message}");
            throw;
        }

        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }

    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
        List<GetTokenHistoryPriceInput> inputs)
    {
        var result = new List<TokenPriceDataDto>();
        try
        {
            foreach (var token in inputs)
            {
                var time = token.DateTime.ToString("dd-MM-yyyy");
                if (token.Symbol.IsNullOrEmpty())
                {
                    result.Add(new TokenPriceDataDto());
                    continue;
                }

                var priceResult = await _tokenPriceService.GetHistoryPriceAsync(token.Symbol.ToLower(), time);
                result.Add(priceResult);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Get history price failed. Error message:{ex.Message}");
            throw;
        }

        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }

    public Task<ContractAddressDto> GetContractAddressAsync()
    {
        return Task.FromResult(new ContractAddressDto
        {
            ContractName = _contractAddressOptions.TokenClaimAddress.ContractName,
            MainChainAddress = _contractAddressOptions.TokenClaimAddress.MainChainAddress,
            SideChainAddress = _contractAddressOptions.TokenClaimAddress.SideChainAddress
        });
    }

    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        //symbol is fuzzy matching
        var chainId = input.ChainIds.Count == 1 ? input.ChainIds.First() : string.Empty;

        var userTokensDto = await _tokenProvider.GetUserTokenInfoListAsync(CurrentUser.GetId(), chainId, string.Empty);
        userTokensDto = userTokensDto?.Where(t => t.Token.Symbol.Contains(input.Symbol.Trim().ToUpper())).ToList();
        var indexerToken =
            await _tokenProvider.GetTokenInfosAsync(chainId, string.Empty, input.Symbol.Trim().ToUpper());

        var tokenInfoList = GetTokenInfoList(userTokensDto, indexerToken.TokenInfo);

        // Check and adjust SkipCount and MaxResultCount
        var skipCount = input.SkipCount < TokensConstants.SkipCountDefault ? TokensConstants.SkipCountDefault : input.SkipCount;
        var maxResultCount = input.MaxResultCount <= TokensConstants.MaxResultCountInvalid ? TokensConstants.MaxResultCountDefault : input.MaxResultCount;

        return tokenInfoList.Skip(skipCount).Take(maxResultCount).ToList();
    }

    public async Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        var tokenIndex =
            await _tokenProvider.GetUserTokenInfoAsync(CurrentUser.GetId(), chainId, symbol.Trim().ToUpper());
        if (tokenIndex != null)
        {
            return ObjectMapper.Map<UserTokenIndex, GetTokenInfoDto>(tokenIndex);
        }

        var dto = await _tokenProvider.GetTokenInfosAsync(chainId, symbol.Trim().ToUpper(), string.Empty, 0, 1);
        var tokenInfo = dto?.TokenInfo?.FirstOrDefault();
        if (tokenInfo == null)
        {
            return await _tokenCacheProvider.GetTokenInfoAsync(chainId, symbol, TokenType.Token);
        }

        return ObjectMapper.Map<IndexerToken, GetTokenInfoDto>(tokenInfo);
    }

    public async Task<TokenExchange> GetAvgLatestExchangeAsync(string fromSymbol, string toSymbol)
    {
        decimal avgExchange;
        if (fromSymbol != toSymbol)
        {
            var names = _exchangeProviders.Values.Select(p => p.Name()).ToList();
            var getExchangeTasks = names.Select(name => GetLatestExchangeAsync(name.ToString(), fromSymbol, toSymbol))
                .ToList();
            var exchangeList = await Task.WhenAll(getExchangeTasks);
            AssertHelper.NotEmpty(exchangeList, "Query exchange of {}_{} failed", fromSymbol, toSymbol);
            avgExchange = exchangeList.Select(ex => ex.Exchange).Average();
        }
        else
        {
            avgExchange = 1;
        }

        return new TokenExchange
        {
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Exchange = avgExchange,
            Timestamp = DateTime.Now.ToUtcMilliSeconds()
        };
    }

    public async Task<TokenExchange> GetLatestExchangeAsync(string exchangeProviderName, string fromSymbol,
        string toSymbol)
    {
        var providerExists =
            _exchangeProviders.TryGetValue(exchangeProviderName, out var exchangeProvider);
        AssertHelper.IsTrue(providerExists, "Provider of {Name} not exists", exchangeProviderName.ToString());

        return await _latestExchange.GetOrAddAsync(
            GrainIdHelper.GenerateGrainId(exchangeProviderName, fromSymbol, toSymbol),
            async () => await exchangeProvider.LatestAsync(fromSymbol, toSymbol),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
            }
        );
    }

    public async Task<TokenExchange> GetHistoryExchangeAsync(string exchangeProviderName, string fromSymbol,
        string toSymbol, DateTime timestamp)
    {
        var providerExists =
            _exchangeProviders.TryGetValue(exchangeProviderName, out var exchangeProvider);
        AssertHelper.IsTrue(providerExists, "Provider of {Name} not exists", exchangeProviderName);

        return await _latestExchange.GetOrAddAsync(
            GrainIdHelper.GenerateGrainId(exchangeProviderName, fromSymbol, toSymbol),
            async () => await exchangeProvider.HistoryAsync(fromSymbol, toSymbol, timestamp.ToUtcMilliSeconds()),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
            }
        );
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

    public async Task<GetTokenAllowancesDto> GetTokenAllowancesAsync(GetAssetsBase input)
    {
        var tokenApproved = await _tokenProvider.GetTokenApprovedAsync("",
            input.CaAddressInfos.Select(t => t.CaAddress).ToList());
        var tokenApprovedList = tokenApproved.CaHolderTokenApproved.Data.Where(t
            => !t.Symbol.IsNullOrWhiteSpace()).ToList();
        if (tokenApprovedList.Count == 0)
        {
            return new GetTokenAllowancesDto();
        }

        List<GetAllowanceDTO> getAllowanceDtos = tokenApprovedList
            .Select(caHolderTokenApprovedDto => new GetAllowanceDTO
            {
                ChainId = caHolderTokenApprovedDto.ChainId,
                Symbol = caHolderTokenApprovedDto.Symbol,
                Owner = input.CaAddressInfos[0].CaAddress,
                Spender = caHolderTokenApprovedDto.Spender
            })
            .ToList();
        var allowanceMap = await GetAllowanceList(getAllowanceDtos);

        var symbolList = tokenApprovedList.Select(t
            => t.Symbol.Replace("-*", "-1")).Distinct().ToList();
        var tokenInfoTasks = symbolList.Select(t =>
                _tokenCacheProvider.GetTokenInfoAsync(CAServerConsts.AElfMainChainId, t, TokenHelper.GetTokenType(t)))
            .ToList();
        var tokenInfoDtos = await tokenInfoTasks.WhenAll();
        var tokenAllowanceList = tokenApprovedList.GroupBy(t => new
        {
            t.ChainId, t.Spender
        }).Select(g => new
        {
            g.Key,
            Items = g.ToList()
        }).Select(t => new TokenAllowance()
        {
            ChainId = t.Key.ChainId,
            ContractAddress = t.Key.Spender,
            SymbolApproveList = t.Items.Select(s => new SymbolApprove()
            {
                Symbol = s.Symbol,
                Amount = allowanceMap.TryGetValue(GetKey(s.Symbol, t.Key.ChainId, t.Key.Spender), out long value) ? value : 0,
                Decimals = tokenInfoDtos.FirstOrDefault(i => i.Symbol == s.Symbol.Replace("-*", "-1")) == null
                    ? 0
                    : tokenInfoDtos.First(i => i.Symbol == s.Symbol.Replace("-*", "-1")).Decimals,
                UpdateTime = s.UpdateTime,
                ImageUrl = _assetsLibraryProvider.buildSymbolImageUrl(s.Symbol),
            }).ToList()
        }).ToList();

        foreach (var tokenAllowance in tokenAllowanceList)
        {
            var tokenSpender = _tokenSpenderOptions.CurrentValue.TokenSpenderList.FirstOrDefault(t
                => t.ChainId == tokenAllowance.ChainId && t.ContractAddress == tokenAllowance.ContractAddress);
            if (tokenSpender != null)
            {
                ObjectMapper.Map(tokenSpender, tokenAllowance);
            }
        }

        tokenAllowanceList.Sort((t1, t2) =>
            (t1.Name.IsNullOrWhiteSpace() ? CommonConstant.UpperZ : t1.Name).CompareTo(t2.Name.IsNullOrWhiteSpace() ? CommonConstant.UpperZ : t2.Name));

        return new GetTokenAllowancesDto
        {
            Data = tokenAllowanceList,
            TotalRecordCount = tokenAllowanceList.Count
        };
    }

    private async Task<Dictionary<string, long>> GetAllowanceList(List<GetAllowanceDTO> dtoList)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var tasks = dtoList.Select(dto => Task.Run(() => GetAllowanceTask(dto)));
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        _logger.LogDebug("GetAllowanceList dtoList count = {0}, spendTime = {1} ms", dtoList.Count, stopwatch.ElapsedMilliseconds);

        return dtoList.ToDictionary(
            dto => GetKey(dto.Symbol, dto.Spender, dto.ChainId),
            dto => dto.Allowance
        );
    }

    private async void GetAllowanceTask(GetAllowanceDTO dto)
    {
        GetAllowanceOutput output = await _contractProvider.GetAllowanceAsync(dto.Symbol, dto.Owner, dto.Spender, dto.ChainId);
        dto.Allowance = output.Allowance;
    }

    private string GetKey(string symbol, string spender, string chainId)
    {
        return $"{symbol}-{spender}-{chainId}";
    }
}