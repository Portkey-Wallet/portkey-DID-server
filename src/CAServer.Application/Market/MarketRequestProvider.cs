using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using CAServer.Commons;
using Newtonsoft.Json;

namespace CAServer.Market;

public class MarketRequestProvider : IMarketRequestProvider
{
    private readonly string _apiKey;
    private readonly string _cryptocurrencyListingsUrl;
    private readonly string _cryptocurrencyTrendingUrl;
    private readonly string _cryptocurrencyExchangeInfoUrl;
    private readonly string _cryptocurrencyQuotesLatestUrl;

    public MarketRequestProvider()
    {
        _apiKey = "b54bcf4d-1bca-4e8e-9a24-22ff2c3d462c";
        _cryptocurrencyListingsUrl = "https://sandbox-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";
        _cryptocurrencyTrendingUrl = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/trending/latest";
        _cryptocurrencyExchangeInfoUrl = "https://pro-api.coinmarketcap.com/v1/exchange/info";
        _cryptocurrencyQuotesLatestUrl = "https://pro-api.coinmarketcap.com/v2/cryptocurrency/quotes/latest";
    }

    public CoinMarketCapResponseDto<List<CryptocurrencyExchangeInfoDto>> GetCryptocurrencyLogo(List<long> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return new CoinMarketCapResponseDto<List<CryptocurrencyExchangeInfoDto>>();
        }
        NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["id"] = string.Join(",", ids);;
        queryString["aux"] = "logo,status";
        string result = InvokeCryptocurrencyListingsApi(_cryptocurrencyExchangeInfoUrl, queryString);
        return JsonConvert.DeserializeObject<CoinMarketCapResponseDto<List<CryptocurrencyExchangeInfoDto>>>(result);
    }

    public CoinMarketCapResponseDto<List<CryptocurrencyQuotesLatestDto>> GetCryptocurrencyQuotesLatest(List<string> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return new CoinMarketCapResponseDto<List<CryptocurrencyQuotesLatestDto>>();
        }
        NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["id"] = string.Join(",", ids);
        queryString["convert"] = "USD";
        string result = InvokeCryptocurrencyListingsApi(_cryptocurrencyQuotesLatestUrl, queryString);
        return JsonConvert.DeserializeObject<CoinMarketCapResponseDto<List<CryptocurrencyQuotesLatestDto>>>(result);
    }
    
    public CoinMarketCapResponseDto<List<CryptocurrencyListingsLatestDto>> GetCryptocurrencyListingsLatestAsync()
    {
        NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["start"] = "1";
        queryString["limit"] = "50"; // 1 .. 5000
        queryString["convert"] = "USD";
        //"name""symbol""date_added""market_cap""market_cap_strict""price""circulating_supply"
        //"total_supply""max_supply""num_market_pairs""volume_24h""percent_change_1h""percent_change_24h"
        //"percent_change_7d""market_cap_by_total_supply_strict""volume_7d""volume_30d"
        queryString["sort"] = "market_cap"; 
        queryString["sort_dir"] = "desc";
        string result = InvokeCryptocurrencyListingsApi(_cryptocurrencyListingsUrl, queryString);
        return JsonConvert.DeserializeObject<CoinMarketCapResponseDto<List<CryptocurrencyListingsLatestDto>>>(result);
    }

    public CoinMarketCapResponseDto<List<CryptocurrencyTrendingLatest>> GetCryptocurrencyTrendingLatestAsync()
    {
        NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["start"] = "1";
        queryString["limit"] = "50"; // 1 .. 1000
        queryString["convert"] = "USD";
        queryString["time_period"] = "24h"; //"24h""30d""7d"
        string result = InvokeCryptocurrencyListingsApi(_cryptocurrencyTrendingUrl, queryString);
        return JsonConvert.DeserializeObject<CoinMarketCapResponseDto<List<CryptocurrencyTrendingLatest>>>(result);
    }
    
    private string InvokeCryptocurrencyListingsApi(string url, NameValueCollection queryParams)
    {
        var URL = new UriBuilder(url);
        URL.Query = queryParams.ToString();

        var client = new WebClient();
        client.Headers.Add("X-CMC_PRO_API_KEY", _apiKey);
        client.Headers.Add("Accepts", "application/json");
        return client.DownloadString(URL.ToString());
    }
}