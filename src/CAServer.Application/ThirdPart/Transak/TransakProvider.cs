using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.Svg;
using CAServer.Grains.Grain.Svg.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.State.ThirdPart;
using CAServer.Http;
using CAServer.Http.Dtos;
using CAServer.Options;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
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
    private readonly ISecretProvider _secretProvider;
    public const int RetryCount = 2;
    public const string ErrorTokenAccessToken = "Access Token";

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .IgnoreNullValue()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .Build();

    public TransakProvider(
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IHttpProvider httpProvider, IOptionsMonitor<RampOptions> rampOptions, IClusterClient clusterClient,
        IAbpDistributedLock distributedLock, ILogger<TransakProvider> logger,
        IImageProcessProvider imageProcessProvider, ISecretProvider secretProvider)
    {
        _thirdPartOptions = thirdPartOptions;
        _httpProvider = httpProvider;
        _rampOptions = rampOptions;
        _clusterClient = clusterClient;
        _distributedLock = distributedLock;
        _logger = logger;
        _imageProcessProvider = imageProcessProvider;
        _secretProvider = secretProvider;
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
            if (_rampOptions?.CurrentValue?.Providers?.TryGetValue(ThirdPartNameType.Transak.ToString(),
                    out var provider) == true)
            {
                var webhookUrl = provider.WebhookUrl;
                if (webhookUrl.IsNullOrEmpty()) return;

                await UpdateWebhookAsync(new UpdateWebhookRequest
                {
                    WebhookURL = webhookUrl
                });
            }
            else
            {
                _logger.LogWarning("Transak webhook url options not exists, skip update to Transak");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "init Transak provider error");
        }
    }

    private async Task<Dictionary<string, string>> GetAccessTokenHeaderAsync()
    {
        var accessToken = await GetAccessTokenWithRetryAsync(true);
        return new Dictionary<string, string> { ["access-token"] = accessToken };
    }

    /// <summary>
    ///     Get Access Token With Retry
    /// </summary>
    /// <param name="force"></param>
    /// <param name="timeOutMillis"></param>
    /// <returns></returns>
    public async Task<string> GetAccessTokenWithRetryAsync(bool force = false, long timeOutMillis = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return await GetAccessTokenWithCacheAsync(force);
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
        var tokenAccessTokenGrain = _clusterClient.GetGrain<ITransakAccessTokenGrain>(apiKey);
        var cacheData = force ? null : (await tokenAccessTokenGrain.GetAccessToken()).Data;

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
        await tokenAccessTokenGrain.SetAccessToken(new TransakAccessTokenDto()
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
        var apiKey = GetApiKey();
        var secret = await _secretProvider.GetSecretWithCacheAsync(apiKey);
        var accessTokenResp = await _httpProvider.InvokeAsync<TransakMetaResponse<object, TransakAccessToken>>(
            TransakOptions().BaseUrl,
            TransakApi.RefreshAccessToken,
            body: JsonConvert.SerializeObject(new Dictionary<string, string> { ["apiKey"] = apiKey }),
            header: new Dictionary<string, string> { ["api-secret"] = secret },
            settings: JsonSerializerSettings, 
            withDebugLog: false,
            withInfoLog: false);
        AssertHelper.NotNull(accessTokenResp?.Data, "AccessToken response null");
        AssertHelper.NotEmpty(accessTokenResp!.Data.AccessToken, "AccessToken empty");
        return accessTokenResp.Data;
    }

    /// <summary>
    ///     Get Crypto Currencies Async
    /// </summary>
    /// <returns></returns>
    public async Task<List<TransakCryptoItem>> GetCryptoCurrenciesAsync()
    {
        var resp = await _httpProvider.InvokeAsync<TransakBaseResponse<List<TransakCryptoItem>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetCryptoCurrencies,
            withInfoLog: false,
            withDebugLog: false
        );
        _logger.LogInformation("Transak GetCryptoCurrenciesAsync response:{0}", JsonConvert.SerializeObject(resp));
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
        var resp = await _httpProvider.InvokeAsync<TransakBaseResponse<List<TransakFiatItem>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetFiatCurrencies,
            header: new Dictionary<string, string>
            {
                ["accept"] = "application/json"
            },
            param: new Dictionary<string, string> { ["apiKey"] = GetApiKey() },
            withInfoLog: false,
            withDebugLog: false
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
        var resp = await _httpProvider.InvokeAsync<TransakBaseResponse<List<TransakCountry>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetCountries,
            withInfoLog: false,
            withDebugLog: false
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
        var resp = await _httpProvider.InvokeAsync<TransakBaseResponse<TransakRampPrice>>(
            TransakOptions().BaseUrl,
            TransakApi.GetPrice,
            param: JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(input, JsonSerializerSettings)),
            withInfoLog: true
        );
        AssertHelper.IsTrue(resp.Success, "GetRampPriceAsync Transak response error, code={Code}, message={Message}",
            resp.Error?.StatusCode, resp.Error?.Message);
        return resp.Response;
    }

    /// <summary>
    ///     update webhook url
    /// </summary>
    /// <param name="input"></param>
    private async Task UpdateWebhookAsync(UpdateWebhookRequest input)
    {
        // retry once
        for (var i = 0; i < RetryCount; i++)
        {
            // Update the webhook address when the system starts.
            var webHookRes = await _httpProvider.InvokeResponseAsync(TransakOptions().BaseUrl, TransakApi.UpdateWebhook,
                body: JsonConvert.SerializeObject(input, JsonSerializerSettings),
                header: await GetAccessTokenHeaderAsync(),
                withInfoLog: true
            );
            AssertHelper.NotNull(webHookRes, "transak webhook http response null");
            //right response return 
            if (webHookRes.StatusCode.Equals(HttpStatusCode.OK))
            {
                return;
            }
            
            //bad response and special treat the question of token access
            var content = await webHookRes.Content.ReadAsStringAsync();

            AssertHelper.IsTrue(!HttpStatusCode.BadRequest.Equals(webHookRes.StatusCode),
                "transak webhook http response exception,ex is {Content}", content);
            AssertHelper.IsTrue(content.Contains(ErrorTokenAccessToken),
                "transak webhook http response exception,ex is{Content}", content);

            await GetAccessTokenWithRetryAsync(true);
        }
    }

    /// <summary>
    ///     Get order by id
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task<TransakOrderDto> GetOrderByIdAsync(string orderId)
    {
        var apiKey = GetApiKey();
        var secret = await _secretProvider.GetSecretWithCacheAsync(apiKey);
        var resp = await _httpProvider.InvokeAsync<QueryTransakOrderByIdResult>(TransakOptions().BaseUrl,
            TransakApi.GetOrderById,
            pathParams: new Dictionary<string, string> { ["orderId"] = orderId },
            header: new Dictionary<string, string> { ["api-secret"] = secret },
            settings: JsonSerializerSettings,
            withInfoLog: true
        );

        return resp?.Data;
    }

    /// <summary>
    ///     if svg in amazon return amazon url else return upload to amazon url
    /// </summary>
    /// <param name="transakFiatItems"></param>
    /// <returns></returns>
    public async Task SetSvgUrlAsync(List<TransakFiatItem> transakFiatItems)
    {
        var uploadTask = new List<Task>();
        foreach (var transakFiatItem in transakFiatItems)
        {
            if (transakFiatItem.Icon.IsNullOrEmpty() || transakFiatItem.Icon.StartsWith("http"))
            {
                transakFiatItem.IconUrl = transakFiatItem.Icon;
                continue;
            }

            uploadTask.Add(SingleSetSvgUrlAsync(transakFiatItem));
        }

        await Task.WhenAll(uploadTask);
    }

    private async Task SingleSetSvgUrlAsync(TransakFiatItem transakFiatItem)
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
        portkeyUrl = portkeyUrl.ReplaceWithDict(new Dictionary<string, string> { ["SVG_MD5"] = svgMd5 });

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