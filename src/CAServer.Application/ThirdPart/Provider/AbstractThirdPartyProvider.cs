using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;

public abstract class AbstractThirdPartyProvider : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    protected readonly JsonSerializerSettings JsonDecodeSettings = new ()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    protected AbstractThirdPartyProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> Invoke<T>(string domain, ApiInfo apiInfo, Dictionary<string, string> param = null, string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null)
    {
        var resp = await Invoke(domain, apiInfo, param, body, header);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{apiInfo.Path}] response body: {resp}", ex);
        }
    }

    public async Task<string> Invoke(string domain, ApiInfo apiInfo, Dictionary<string, string> param = null, string body = null,
        Dictionary<string, string> header = null)
    {
        // url params
        var fullUrl = domain + apiInfo.Path;
        var builder = new UriBuilder(fullUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var item in param ?? new Dictionary<string, string>())
            query[item.Key] = item.Value;
        builder.Query = query.ToString() ?? string.Empty;
        
        var request = new HttpRequestMessage(apiInfo.Method, builder.ToString());

        // headers
        foreach (var h in header ?? new Dictionary<string, string>())
            request.Headers.Add(h.Key, h.Value);
        
        // body
        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        // send
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Server [{fullUrl}] returned status code {response.StatusCode} : {errorContent}");
        }
        
        return await response.Content.ReadAsStringAsync();
    }
}

public class ApiInfo
{
    public string Path { get; set; }
    public HttpMethod Method { get; set; }
    public string Name { get; set; }

    public ApiInfo(HttpMethod method, string path, string name = null)
    {
        Path = path;
        Method = method;
        Name = name;
    }
    
    public ApiInfo PathParam(Dictionary<string, string> pathParams)
    {
        var newPath = pathParams.Aggregate(Path, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
        return new ApiInfo(Method, newPath, Name);
    }
    
}