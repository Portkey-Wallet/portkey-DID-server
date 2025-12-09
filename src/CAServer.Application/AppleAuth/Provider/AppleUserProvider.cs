using System;
using System.Net;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace CAServer.AppleAuth.Provider;

public class AppleUserProvider : IAppleUserProvider, ISingletonDependency
{
    private IDatabase Db { get; set; }

    private const string Key = "AppleUserExtraInfo";
    private readonly PortkeyV1Options _options;
    private readonly ILogger<AppleUserProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AppleUserProvider(IOptions<AppleCacheOptions> cacheOptions,
        IOptionsSnapshot<PortkeyV1Options> options,
        ILogger<AppleUserProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        var redisConnection = ConnectionMultiplexer.Connect(cacheOptions.Value.Configuration);
        Db = redisConnection.GetDatabase(cacheOptions.Value.Db);
    }

    public async Task SetUserExtraInfoAsync(AppleUserExtraInfo userExtraInfo)
    {
        await Db.HashSetAsync(Key, userExtraInfo.UserId, JsonConvert.SerializeObject(userExtraInfo));
    }

    public async Task<AppleUserExtraInfo> GetUserExtraInfoAsync(string userId)
    {
        var userInfo = await Db.HashGetAsync(Key, userId);

        if (!userInfo.HasValue) return null;
        return JsonConvert.DeserializeObject<AppleUserExtraInfo>(userInfo);
    }

    public async Task<bool> UserExtraInfoExistAsync(string userId) => await Db.HashExistsAsync(Key, userId);

    public async Task<UserExtraInfoResultDto> GetUserInfoAsync(string userId)
    {
        if (_options.BaseUrl.IsNullOrEmpty()) return null;
        
        var url = $"{_options.BaseUrl}/{CommonConstant.GetUserExtraInfoUri}/{userId}";
        return await GetUserExtraInfoAsync<UserExtraInfoResultDto>(url);
    }

    private async Task<T> GetUserExtraInfoAsync<T>(string url)
    {
        var response = await _httpClientFactory.CreateClient().GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            return default;
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            throw new UserFriendlyException(content, response.StatusCode.ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }
}