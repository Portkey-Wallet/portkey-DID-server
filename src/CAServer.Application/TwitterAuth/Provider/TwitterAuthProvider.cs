using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.TwitterAuth.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.TwitterAuth.Provider;

public interface ITwitterAuthProvider
{
    Task<TwitterUserInfoDto> GetUserInfoAsync(string url, Dictionary<string, string> headers);
}

public class TwitterAuthProvider : ITwitterAuthProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwitterAuthProvider> _logger;

    public TwitterAuthProvider(IHttpClientFactory httpClientFactory, ILogger<TwitterAuthProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TwitterUserInfoDto> GetUserInfoAsync(string url, Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient();
        foreach (var keyValuePair in headers)
        {
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<TwitterUserInfoDto>(content);
    }
}