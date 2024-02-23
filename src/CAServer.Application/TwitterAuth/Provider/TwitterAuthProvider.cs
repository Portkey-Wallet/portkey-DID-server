using System.Collections.Generic;
using System.Linq;
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

    Task<string> PostFormAsync(string url, Dictionary<string, string> paramDic,
        Dictionary<string, string> headers);
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
    
    public async Task<string> PostFormAsync(string url, Dictionary<string, string> paramDic,
        Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var param = new List<KeyValuePair<string, string>>();
        if (paramDic is { Count: > 0 })
        {
            param.AddRange(paramDic.ToList());
        }

        var response = await client.PostAsync(url, new FormUrlEncodedContent(param));
        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramDic));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return content;
    }
}