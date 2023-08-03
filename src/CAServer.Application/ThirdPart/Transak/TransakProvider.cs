using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly.RateLimit;
using RabbitMQ.Client;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Modularity;

namespace CAServer.ThirdPart.Provider;

public static class TransakApi
{
    public static readonly ApiInfo GetWebhooks = new(HttpMethod.Get, "/partners/api/v2/webhooks");
    public static readonly ApiInfo GetOrderById = new(HttpMethod.Get, "/partners/api/v2/order/{orderId}");
    public static readonly ApiInfo GetCountries = new(HttpMethod.Get, "/api/v2/countries");
    public static readonly ApiInfo GetCryptoCurrencies = new(HttpMethod.Get, "/api/v2/currencies/crypto-currencies");
    public static readonly ApiInfo GetFiatCurrencies = new(HttpMethod.Get, "/api/v2/currencies/fiat-currencies");
    public static readonly ApiInfo GetPrice = new(HttpMethod.Get, "/api/v2/currencies/price");

    public static readonly ApiInfo UpdateWebhook = new(HttpMethod.Post, "/partners/api/v2/update-webhook-url");
    public static readonly ApiInfo RefreshAccessToken = new(HttpMethod.Post, "/partners/api/v2/refresh-token");
}

public class TransakProvider : AbstractThirdPartyProvider
{
    private readonly TransakOptions _transakOptions;
    private readonly ICacheProvider _cacheProvider;
    private readonly IAbpDistributedLock _distributedLock;


    public TransakProvider(IOptions<ThirdPartOptions> thirdPartOptions, ICacheProvider cacheProvider,
        IHttpClientFactory httpClientFactory, IAbpDistributedLock distributedLock) : base(httpClientFactory)
    {
        _cacheProvider = cacheProvider;
        _distributedLock = distributedLock;
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
            body: JsonConvert.SerializeObject(new Dictionary<string, string> { ["webhookURL"] = _transakOptions.WebhookUrl }),
            header : new Dictionary<string, string>{ ["access-token"] = accessToken});
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

    public async Task<string> GetAccessTokenAsync(bool force = false)
    {
        var apiKey = GetApiKey();
        var cacheKey = CacheKey(apiKey);
        string cacheData = force ? string.Empty : await _cacheProvider.Get(cacheKey);
        if (!cacheData.IsNullOrEmpty()) return cacheData;

        await using var handle = await _distributedLock.TryAcquireAsync(cacheKey);
        if (handle == null)
            throw new RateLimitRejectedException(TimeSpan.Zero);

        // cacheData not exists or force
        var accessTokenResp = await Invoke<TransakAccessTokenResp>(_transakOptions.BaseUrl, TransakApi.RefreshAccessToken,
            body: JsonConvert.SerializeObject(new Dictionary<string, string> { ["apiKey"] = apiKey }),
            header: new Dictionary<string, string> { ["api-secret"] = _transakOptions.AppSecret },
            settings:JsonDecodeSettings);
        if (accessTokenResp?.Data == null || accessTokenResp.Data.AccessToken.IsNullOrEmpty())
            throw new UserFriendlyException("Internal error, please try again later");

        // Expire ahead of 1/5 of the totalDuration.
        var expiration = DateTimeOffset.FromUnixTimeSeconds(accessTokenResp.Data.ExpiresAt).UtcDateTime;
        var totalDuration = expiration - DateTime.UtcNow;
        var earlyDuration = totalDuration * 0.8;
        await _cacheProvider.Set(cacheKey, accessTokenResp.Data.AccessToken, earlyDuration);

        return accessTokenResp.Data.AccessToken;
    }
    
    public async Task<TransakOrderDto> GetOrderById(string orderId)
    {
        var resp = await Invoke<QueryTransakOrderByIdResult>(_transakOptions.BaseUrl,
            TransakApi.GetOrderById.PathParam(new Dictionary<string, string> { ["orderId"] = orderId }),
            header: new Dictionary<string, string> { ["api-secret"] = _transakOptions.AppSecret },
            settings:JsonDecodeSettings
            );
        
        return resp?.Data;
    }
}