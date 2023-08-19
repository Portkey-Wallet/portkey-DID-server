using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CAServer.Common;

public class HttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await _httpClientFactory.CreateClient().GetStringAsync(url);
        return JsonConvert.DeserializeObject<T>(response);
    }
    
    public async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient();
        foreach (var keyValuePair in headers)
        {
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var response = await client.GetStringAsync(url);
        return JsonConvert.DeserializeObject<T>(response);
    }
}