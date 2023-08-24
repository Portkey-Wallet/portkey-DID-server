using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Polly.RateLimit;
using Volo.Abp;
using Volo.Abp.DistributedLocking;

namespace CAServer.ThirdPart.Provider;

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

public class TransakProvider : AbstractThirdPartyProvider
{
    private readonly TransakOptions _transakOptions;
    private readonly ICacheProvider _cacheProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TransakProvider> _logger;


    public TransakProvider(IOptions<ThirdPartOptions> thirdPartOptions, ICacheProvider cacheProvider,
        IHttpClientFactory httpClientFactory, IAbpDistributedLock distributedLock, IClusterClient clusterClient,
        ILogger<TransakProvider> logger) : base(httpClientFactory, logger)
    {
        _cacheProvider = cacheProvider;
        _distributedLock = distributedLock;
        _clusterClient = clusterClient;
        _logger = logger;
        _transakOptions = thirdPartOptions.Value.transak;
        InitAsync().GetAwaiter().GetResult();
    }

    private async Task InitAsync()
    {
        // The webhook address needs to be MANUALLY updated in the testing environment to prevent the incorrect use of the prod-API-key.
        if (EnvHelper.IsDevelopment()) return;

        // Update the webhook address when the system starts.
        var accessToken = await GetAccessTokenWithRetry();
        await Invoke(_transakOptions.BaseUrl, TransakApi.UpdateWebhook,
            body: JsonConvert.SerializeObject(new Dictionary<string, string>
                { ["webhookURL"] = _transakOptions.WebhookUrl }),
            header: new Dictionary<string, string> { ["access-token"] = accessToken });
    }

    public string GetApiKey()
    {
        var key = (_transakOptions.AppId ?? "").Split(":");
        return key.Length == 1 ? key[0] : key[1];
    }

    private static string CacheKey(string apiKey)
    {
        return $"ramp:transak:access_token:{apiKey}";
    }


    public async Task<string> GetAccessTokenWithRetry(long timeOutMillis = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return await GetAccessTokenAsync();
            }
            catch (RateLimitRejectedException ex)
            {
                if (stopwatch.ElapsedMilliseconds > timeOutMillis)
                    throw new UserFriendlyException("Failed to get access token within the specified timeout.");
            }

            await Task.Delay(100);
        }
    }

    public async Task<string> GetAccessTokenAsync(bool force = false, bool containsExpire = true)
    {
        var now = DateTime.UtcNow;
        var apiKey = GetApiKey();
        var cacheKey = CacheKey(apiKey);
        var tokenGrain = _clusterClient.GetGrain<ITransakGrain>(apiKey);
        var cacheData = force ? null : (await tokenGrain.GetAccessToken()).Data;

        var hasData = cacheData != null && !cacheData.AccessToken.IsNullOrEmpty();
        if (hasData && containsExpire)
            return cacheData.AccessToken;
        if (hasData && cacheData.RefreshTime > DateTime.UtcNow)
            return cacheData.AccessToken;

        // Use a distributed lock to prevent duplicate refreshes during concurrent access.
        await using var handle = await _distributedLock.TryAcquireAsync(cacheKey);
        if (handle == null)
            throw new RateLimitRejectedException(TimeSpan.Zero);

        // cacheData not exists or force
        var accessTokenResp = await Invoke<TransakAccessTokenResp>(_transakOptions.BaseUrl,
            TransakApi.RefreshAccessToken,
            body: JsonConvert.SerializeObject(new Dictionary<string, string> { ["apiKey"] = apiKey }),
            header: new Dictionary<string, string> { ["api-secret"] = _transakOptions.AppSecret },
            settings: JsonSettings);
        if (accessTokenResp?.Data == null || accessTokenResp.Data.AccessToken.IsNullOrEmpty())
            throw new UserFriendlyException("Internal error, please try again later");

        // Expire ahead of RefreshTokenDurationPercent of the totalDuration.
        var expiration = DateTimeOffset.FromUnixTimeSeconds(accessTokenResp.Data.ExpiresAt).UtcDateTime;
        var refreshDuration = (expiration - now) * _transakOptions.RefreshTokenDurationPercent;

        // record accessToken data to Grain
        await tokenGrain.SetAccessToken(new TransakAccessTokenDto()
        {
            AccessToken = accessTokenResp.Data.AccessToken,
            ExpireTime = expiration,
            RefreshTime = now + refreshDuration
        });

        return accessTokenResp.Data.AccessToken;
    }

    public async Task<TransakOrderDto> GetOrderById(string orderId)
    {
        var resp = await Invoke<QueryTransakOrderByIdResult>(_transakOptions.BaseUrl, TransakApi.GetOrderById,
            pathParams:new Dictionary<string, string> { ["orderId"] = orderId },
            header: new Dictionary<string, string> { ["api-secret"] = _transakOptions.AppSecret },
            settings: JsonSettings
        );
        
        return resp?.Data;
    }
}