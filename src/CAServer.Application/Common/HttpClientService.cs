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

    public Task<T> GetAsync<T>(string url, IDictionary<string, string> headers)
    {
        throw new System.NotImplementedException();
    }

    public Task<T> PostAsync<T>(string url)
    {
        throw new System.NotImplementedException();
    }

    public Task<T> PostAsync<T>(string url, object paramObj)
    {
        throw new System.NotImplementedException();
    }

    public Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        throw new System.NotImplementedException();
    }

    public Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj, Dictionary<string, string> headers)
    {
        throw new System.NotImplementedException();
    }
    
    public class RequestMediaType
    {
        
    }
}