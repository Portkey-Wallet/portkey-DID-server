using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CAServer.Options;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
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
    private readonly IClusterClient _clusterClient;
    private readonly ContractAddressOptions _contractAddressOptions;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDistributedCache<TokenExchange> _latestExchange;
    private readonly IDistributedCache<TokenExchange> _historyExchange;
    private readonly Dictionary<string, IExchangeProvider> _exchangeProviders;
    private readonly ITokenCacheProvider _tokenCacheProvider;

    public TokenAppService(IClusterClient clusterClient, IOptions<ContractAddressOptions> contractAddressesOptions,
        ITokenProvider tokenProvider, IEnumerable<IExchangeProvider> exchangeProviders,
        IDistributedCache<TokenExchange> latestExchange,
        IDistributedCache<TokenExchange> historyExchange,
        ITokenCacheProvider tokenCacheProvider)
    {
        _clusterClient = clusterClient;
        _tokenProvider = tokenProvider;
        _latestExchange = latestExchange;
        _historyExchange = historyExchange;
        _contractAddressOptions = contractAddressesOptions.Value;
        _exchangeProviders = exchangeProviders.ToDictionary(p => p.Name().ToString(), p => p);
        _tokenCacheProvider = tokenCacheProvider;
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
                var grainId = GrainIdHelper.GenerateGrainId(symbol);
                var grain = _clusterClient.GetGrain<ITokenPriceGrain>(grainId);
                var priceResult = await grain.GetCurrentPriceAsync(symbol);
                if (!priceResult.Success)
                {
                    throw new UserFriendlyException(priceResult.Message);
                }

                result.Add(priceResult.Data);
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

                var grainId = GrainIdHelper.GenerateGrainId(token.Symbol.ToLower(), time);
                var grain = _clusterClient.GetGrain<ITokenPriceSnapshotGrain>(grainId);
                var priceResult = await grain.GetHistoryPriceAsync(token.Symbol.ToLower(), time);
                if (!priceResult.Success)
                {
                    throw new UserFriendlyException(priceResult.Message);
                }

                result.Add(priceResult.Data);
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

        return GetTokenInfoList(userTokensDto, indexerToken.TokenInfo);
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
            var getExchangeTasks = names.Select(name => GetLatestExchangeAsync(name.ToString(), fromSymbol, toSymbol)).ToList();
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
}