using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Tokens.TokenPrice.Provider.FeiXiaoHao;

public class FeiXiaoHaoTokenPriceProvider : ITokenPriceProvider, ISingletonDependency
{
    private readonly ILogger<FeiXiaoHaoTokenPriceProvider> _logger;
    private readonly IOptionsMonitor<FeiXiaoHaoOptions> _feiXiaoHaoOptions;
    private readonly IHttpClientProvider _httpClientProvider;

    private const string UsdSymbol = "USD";
    private const string HeaderStringAccepts = "Accepts";
    private const string HeaderStringAcceptsValue = "application/json";
    private const string QueryStringStart = "start";
    private const string QueryStringLimit = "limit";
    private const string QueryStringConvert = "convert";

    public FeiXiaoHaoTokenPriceProvider(ILogger<FeiXiaoHaoTokenPriceProvider> logger,
        IOptionsMonitor<FeiXiaoHaoOptions> feiXiaoHaoOptions, IHttpClientProvider httpClientProvider)
    {
        _logger = logger;
        _feiXiaoHaoOptions = feiXiaoHaoOptions;
        _httpClientProvider = httpClientProvider;
    }

    public async Task<decimal> GetPriceAsync(string symbol)
    {
        var decimals = await GetPriceAsync(new string[] { symbol });
        if (decimals == null || !decimals.ContainsKey(symbol))
        {
            throw new UserFriendlyException("No price information found");
        }

        return decimals[symbol];
    }

    public async Task<Dictionary<string, decimal>> GetPriceAsync(params string[] symbols)
    {
        if (symbols.IsNullOrEmpty())
        {
            return null;
        }

        try
        {
            var pageNo = 0;
            var pageSize = _feiXiaoHaoOptions.CurrentValue.PageSize;
            var maxPageNo = _feiXiaoHaoOptions.CurrentValue.MaxPageNo;
            var headers = new Dictionary<string, string> { { HeaderStringAccepts, HeaderStringAcceptsValue } };
            var parameters = new Dictionary<string, string>
            {
                { QueryStringStart, pageNo.ToString() }, { QueryStringLimit, pageSize.ToString() } //,
                //{ QueryStringConvert, UsdSymbol }
            };
            var prices = new Dictionary<string, decimal>();
            var symbolSet = symbols.ToHashSet();
            do
            {
                var feiXiaoHaoResponseDto = await _httpClientProvider.GetAsync<List<FeiXiaoHaoTokenInfo>>(
                    _feiXiaoHaoOptions.CurrentValue.BaseUrl, headers, parameters,
                    _feiXiaoHaoOptions.CurrentValue.Timeout);
                if (feiXiaoHaoResponseDto == null || feiXiaoHaoResponseDto.IsNullOrEmpty())
                {
                    break;
                }

                await GetTokenPriceAsync(symbolSet, feiXiaoHaoResponseDto, prices);

                if (feiXiaoHaoResponseDto.Count < pageSize || symbolSet.IsNullOrEmpty())
                {
                    break;
                }

                pageNo++;
                parameters[QueryStringStart] = (pageNo * pageSize).ToString();
            } while (pageNo <= maxPageNo);

            if (symbolSet.IsNullOrEmpty())
            {
                return prices;
            }

            _logger.LogError("Can not get current price from 'FeiXiaoHao', {0}", JsonConvert.SerializeObject(symbols));
            throw new UserFriendlyException("Can not get current price from 'FeiXiaoHao'",
                JsonConvert.SerializeObject(symbols));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get current price from 'FeiXiaoHao'");
            throw;
        }
    }

    private Task GetTokenPriceAsync(ICollection<string> symbols,
        IEnumerable<FeiXiaoHaoTokenInfo> responseDto, IDictionary<string, decimal> prices)
    {
        foreach (var tokenInfo in responseDto.Where(tokenInfo => symbols.Contains(tokenInfo.Symbol)))
        {
            prices.Add(tokenInfo.Symbol, tokenInfo.PriceUsd);
            symbols.Remove(tokenInfo.Symbol);
        }

        return Task.CompletedTask;
    }

    public Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        throw new NotSupportedException(" 'FeiXiaoHao' does not support historical price query");
    }

    public bool IsAvailable()
    {
        return _feiXiaoHaoOptions.CurrentValue.IsAvailable;
    }

    public int GetPriority()
    {
        return _feiXiaoHaoOptions.CurrentValue.Priority;
    }
    
    public async Task<List<CoinMarkets>> GetHotListingsAsync()
    {
        throw new NotSupportedException(" 'FeiXiaoHao' does not support hot listings query");
    }

    public async Task<List<CoinMarkets>> GetCoinMarketsByCoinIdsAsync(string[] ids, int perPage)
    {
        throw new NotSupportedException(" 'FeiXiaoHao' does not support markets by coin ids query");
    }

    public async Task<TrendingList> GetTrendingListingsAsync()
    {
        throw new NotSupportedException(" 'FeiXiaoHao' does not support trendings query");
    }

    public async Task<MarketChartById> GetMarketChartsByCoinIdAsync(string coinId, string vsCurrency, string days)
    {
        throw new NotSupportedException(" 'FeiXiaoHao' does not support market chart query");
    }
}