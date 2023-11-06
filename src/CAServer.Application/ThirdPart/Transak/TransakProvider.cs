using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Common.Dtos;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.State.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos.ThirdPart;
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

public class TransakProvider : CAServerAppService
{
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IHttpProvider _httpProvider;
    private readonly IClusterClient _clusterClient;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .IgnoreNullValue()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .Build();

    public TransakProvider(
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IHttpProvider httpProvider, IOptionsMonitor<RampOptions> rampOptions, IClusterClient clusterClient,
        IAbpDistributedLock distributedLock)
    {
        _thirdPartOptions = thirdPartOptions;
        _httpProvider = httpProvider;
        _rampOptions = rampOptions;
        _clusterClient = clusterClient;
        _distributedLock = distributedLock;
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
        if(_rampOptions?.CurrentValue?.Providers.TryGetValue(ThirdPartNameType.Alchemy.ToString(), out var provider) == true)
        {
            var webhookUrl = provider.WebhookUrl;
            AssertHelper.NotEmpty(webhookUrl, "Transak webhookUrl empty in ramp options");
            await UpdateWebhookAsync(new UpdateWebhookRequest
            {
                WebhookUrl = webhookUrl
            });
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
                return await GetAccessTokenWithCacheAsync();
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
            TransakApi.GetFiatCurrencies
        );
        AssertHelper.IsTrue(resp.Success,
            "GetCryptoCurrenciesAsync Transak response error, code={Code}, message={Message}", resp.Error.StatusCode,
            resp.Error.Message);
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
            param: new Dictionary<string, string> { ["apiKey"] = GetApiKey() }
        );
        AssertHelper.IsTrue(resp.Success, "GetFiatCurrencies Transak response error, code={Code}, message={Message}",
            resp.Error.StatusCode, resp.Error.Message);
        return resp.Response;
    }

    /// <summary>
    ///     Get transak country list
    /// </summary>
    /// <returns></returns>
    public async Task<List<TransakCountry>> GetTransakCountriesAsync()
    {
        var resp = await _httpProvider.Invoke<TransakBaseResponse<List<TransakCountry>>>(
            TransakOptions().BaseUrl,
            TransakApi.GetFiatCurrencies
        );
        AssertHelper.IsTrue(resp.Success,
            "GetTransakCountriesAsync Transak response error, code={Code}, message={Message}", resp.Error.StatusCode,
            resp.Error.Message);
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
            resp.Error.StatusCode, resp.Error.Message);
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
            body: JsonConvert.SerializeObject(input),
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
}