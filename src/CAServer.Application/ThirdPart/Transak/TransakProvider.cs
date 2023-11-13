using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Common.Dtos;
using CAServer.Commons;
using CAServer.Grains.Grain.Svg;
using CAServer.Grains.Grain.Svg.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.State.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Polly.RateLimit;
using Volo.Abp.DistributedLocking;

namespace CAServer.ThirdPart.Transak;

public static class TransakApi
{
    public static ApiInfo GetWebhooks { get; } = new(HttpMethod.Get, "/partners/api/v2/webhooks");
    public static ApiInfo GetOrderById { get; } = new(HttpMethod.Get, "/partners/api/v2/order/{orderId}");
    public static ApiInfo GetCountries { get; } = new(HttpMethod.Get, "/api/v2/countries");
    public static ApiInfo GetCryptoCurrencies { get; } = new(HttpMethod.Get, "/api/v2/currencies/crypto-currencies");
    public static ApiInfo GetFiatCurrencies { get; } = new(HttpMethod.Get, "/api/v2/currencies/fiat-currencies");
    public static ApiInfo GetPrice { get; } = new(HttpMethod.Get, "/api/v2/currencies/price");

    public static ApiInfo UpdateWebhook { get; } = new(HttpMethod.Post, "/partners/api/v2/update-webhook-url");
    public static ApiInfo RefreshAccessToken { get; } = new(HttpMethod.Post, "/partners/api/v2/refresh-token");
}

public class TransakProvider
{
    private readonly ILogger<TransakProvider> _logger;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IHttpProvider _httpProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IImageProcessProvider _imageProcessProvider;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .IgnoreNullValue()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .Build();

    public TransakProvider(
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IHttpProvider httpProvider, IOptionsMonitor<RampOptions> rampOptions, IClusterClient clusterClient,
        IAbpDistributedLock distributedLock, ILogger<TransakProvider> logger, IImageProcessProvider imageProcessProvider)
    {
        _thirdPartOptions = thirdPartOptions;
        _httpProvider = httpProvider;
        _rampOptions = rampOptions;
        _clusterClient = clusterClient;
        _distributedLock = distributedLock;
        _logger = logger;
        _imageProcessProvider = imageProcessProvider;
        InitAsync().GetAwaiter().GetResult();
    }

    private TransakOptions TransakOptions()
    {
        return _thirdPartOptions.CurrentValue.Transak;
    }

    public string GetApiKey()
    {
        var key = (TransakOptions().AppId ?? "").Split(":");
        return key.Length == 1 ? key[0] : key[1];
    }

    private static string AccessTokenCacheKey(string apiKey)
    {
        return $"ramp:transak:access_token:{apiKey}";
    }

    private async Task InitAsync()
    {
        try
        {
            if(_rampOptions?.CurrentValue?.Providers.TryGetValue(ThirdPartNameType.Transak.ToString(), out var provider) == true)
            {
                var webhookUrl = provider.WebhookUrl;
                AssertHelper.NotEmpty(webhookUrl, "Transak webhookUrl empty in ramp options");
                await UpdateWebhookAsync(new UpdateWebhookRequest
                {
                    WebhookURL = webhookUrl
                });
            }
            else
            {
                _logger.LogError("Transak webhook url options not exists, skip update to Transak");            
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "init Transak provider error");
        }

    }

    private async Task<Dictionary<string, string>> GetAccessTokenHeader()
    {
        var accessToken = await GetAccessTokenWithRetry();
        return new Dictionary<string, string> { ["access-token"] = accessToken };
    }

    /// <summary>
    ///     Get Access Token With Retry
    /// </summary>
    /// <param name="timeOutMillis"></param>
    /// <returns></returns>
    public async Task<string> GetAccessTokenWithRetry(long timeOutMillis = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return await GetAccessTokenWithCacheAsync(true);
            }
            catch (RateLimitRejectedException ex)
            {
                AssertHelper.IsTrue(stopwatch.ElapsedMilliseconds > timeOutMillis,
                    "Failed to get access token within the specified timeout.");
            }

            await Task.Delay(100);
        }
    }

    // get access token with cache
    private async Task<string> GetAccessTokenWithCacheAsync(bool force = false, bool containsExpire = true)
    {
        var now = DateTime.UtcNow;
        var apiKey = GetApiKey();
        var cacheKey = AccessTokenCacheKey(apiKey);
        var tokenGrain = _clusterClient.GetGrain<ITransakGrain>(apiKey);
        var cacheData = force ? null : (await tokenGrain.GetAccessToken()).Data;

        var hasData = cacheData != null && !cacheData.AccessToken.IsNullOrEmpty();
        if (hasData && (containsExpire || cacheData.RefreshTime > DateTime.UtcNow))
            return cacheData.AccessToken;

        // Use a distributed lock to prevent duplicate refreshes during concurrent access.
        await using var handle = await _distributedLock.TryAcquireAsync(cacheKey);
        if (handle == null)
            throw new RateLimitRejectedException(TimeSpan.Zero);

        // cacheData not exists or force
        var accessToken = await GetAccessTokenAsync();

        // Expire ahead of RefreshTokenDurationPercent of the totalDuration.
        var expiration = DateTimeOffset.FromUnixTimeSeconds(accessToken.ExpiresAt).UtcDateTime;
        var refreshDuration = (expiration - now) * TransakOptions().RefreshTokenDurationPercent;

        // record accessToken data to Grain
        await tokenGrain.SetAccessToken(new TransakAccessTokenDto()
        {
            AccessToken = accessToken.AccessToken,
            ExpireTime = expiration,
            RefreshTime = now + refreshDuration
        });

        return accessToken.AccessToken;
    }


    /// <summary>
    ///     Get AccessToken
    /// </summary>
    /// <returns></returns>
    public async Task<TransakAccessToken> GetAccessTokenAsync()
    {
        // cacheData not exists or force
        var accessTokenResp = await _httpProvider.Invoke<TransakMetaResponse<object, TransakAccessToken>>(
            TransakOptions().BaseUrl,
            TransakApi.RefreshAccessToken,
            body: JsonConvert.SerializeObject(new Dictionary<string, string> { ["apiKey"] = GetApiKey() }),
            header: new Dictionary<string, string> { ["api-secret"] = TransakOptions().AppSecret },
            settings: JsonSerializerSettings);
        AssertHelper.NotNull(accessTokenResp?.Data, "AccessToken response null");
        AssertHelper.NotEmpty(accessTokenResp.Data.AccessToken, "AccessToken empty");
        return accessTokenResp.Data;
    }

    /// <summary>
    ///     Get Crypto Currencies Async
    /// </summary>
    /// <returns></returns>
    public async Task<List<TransakCryptoItem>> GetCryptoCurrenciesAsync()
    {
        var resp = await _httpProvider.Invoke<TransakBaseResponse<List<TransakCryptoItem>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetCryptoCurrencies,
            debugLog: false
        );
        AssertHelper.IsTrue(resp.Success,
            "GetCryptoCurrenciesAsync Transak response error, code={Code}, message={Message}", resp.Error?.StatusCode,
            resp.Error?.Message);
        return resp.Response;
    }


    /// <summary>
    ///     Query fiat currencies
    /// </summary>
    /// <returns></returns>
    public async Task<List<TransakFiatItem>> GetFiatCurrenciesAsync()
    {
        var resp = await _httpProvider.Invoke<TransakBaseResponse<List<TransakFiatItem>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetFiatCurrencies,
            header: new Dictionary<string, string>
            {
                ["accept"] = "application/json"
            },
            param: new Dictionary<string, string> { ["apiKey"] = GetApiKey() },
            debugLog: false
        );
        AssertHelper.IsTrue(resp.Success, "GetFiatCurrencies Transak response error, code={Code}, message={Message}",
            resp.Error?.StatusCode, resp.Error?.Message);
        return resp.Response;
    }

    /// <summary>
    ///     Get transak country list
    /// </summary>
    /// <returns></returns>
    /// <returns></returns>
    public async Task<List<TransakCountry>> GetTransakCountriesAsync()
    {
        var resp = await _httpProvider.Invoke<TransakBaseResponse<List<TransakCountry>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetCountries,
            debugLog: false
        );
        AssertHelper.IsTrue(resp.Success,
            "GetTransakCountriesAsync Transak response error, code={Code}, message={Message}", resp.Error?.StatusCode,
            resp.Error?.Message);
        return resp.Response;
    }


    /// <summary>
    ///     Get ramp price
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<TransakRampPrice> GetRampPriceAsync(GetRampPriceRequest input)
    {
        input.PartnerApiKey = GetApiKey();
        var resp = await _httpProvider.Invoke<TransakBaseResponse<TransakRampPrice>>(
            TransakOptions().BaseUrl,
            TransakApi.GetPrice,
            param: JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(input, JsonSerializerSettings)),
            withLog: true
        );
        AssertHelper.IsTrue(resp.Success, "GetRampPrice Transak response error, code={Code}, message={Message}",
            resp.Error?.StatusCode, resp.Error?.Message);
        return resp.Response;
    }

    /// <summary>
    ///     update webhook url
    /// </summary>
    /// <param name="input"></param>
    public async Task UpdateWebhookAsync(UpdateWebhookRequest input)
    {
        // Update the webhook address when the system starts.
        await _httpProvider.Invoke(TransakOptions().BaseUrl, TransakApi.UpdateWebhook,
            body: JsonConvert.SerializeObject(input, JsonSerializerSettings),
            header: await GetAccessTokenHeader(),
            withLog: true
        );
    }

    /// <summary>
    ///     Get order by id
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task<TransakOrderDto> GetOrderByIdAsync(string orderId)
    {
        var resp = await _httpProvider.Invoke<QueryTransakOrderByIdResult>(TransakOptions().BaseUrl,
            TransakApi.GetOrderById,
            pathParams: new Dictionary<string, string> { ["orderId"] = orderId },
            header: new Dictionary<string, string> { ["api-secret"] = TransakOptions().AppSecret },
            settings: JsonSerializerSettings,
            withLog: true
        );

        return resp?.Data;
    }

    /// <summary>
    ///     if svg in amazon return amazon url else return upload to amazon url
    /// </summary>
    /// <param name="transakFiatItems"></param>
    /// <returns></returns>
    public async Task SetSvgUrl(List<TransakFiatItem> transakFiatItems)
    {
        var uploadTask = new List<Task>(); 
        foreach (var transakFiatItem in transakFiatItems)
        {
            if (transakFiatItem.Icon.IsNullOrEmpty() || transakFiatItem.Icon.StartsWith("http"))
            {
                transakFiatItem.IconUrl = transakFiatItem.Icon;
                continue;
            }
            uploadTask.Add(SingleSetSvgUrl(transakFiatItem));
        }
        await Task.WhenAll(uploadTask);
    }

    private async Task SingleSetSvgUrl(TransakFiatItem transakFiatItem)
    {
        var svgUrl = transakFiatItem.Icon;
        if (string.IsNullOrWhiteSpace(svgUrl))
        {
            return;
        }
        var svgMd5 = EncryptionHelper.MD5Encrypt32(svgUrl);
        var grain = _clusterClient.GetGrain<ISvgGrain>(svgMd5);
        var svgGrain = await grain.GetSvgAsync();
        var portkeyUrl = _rampOptions.CurrentValue.Providers[ThirdPartNameType.Transak.ToString()].CountryIconUrl;
        portkeyUrl.ReplaceWithDict(new Dictionary<string, string> { ["SVG_MD5"] = svgMd5 });
        
        transakFiatItem.IconUrl = svgGrain.AmazonUrl.DefaultIfEmpty(portkeyUrl);
        if (svgGrain.AmazonUrl.NotNullOrEmpty())
        {
            return;
        }

        //async upload to Amazon
        //uploadTask.Add(_imageProcessProvider.UploadSvgAsync(svgMd5, transakFiatItem.Icon));
        _ = await _imageProcessProvider.UploadSvgAsync(svgMd5, transakFiatItem.Icon);
    }
}