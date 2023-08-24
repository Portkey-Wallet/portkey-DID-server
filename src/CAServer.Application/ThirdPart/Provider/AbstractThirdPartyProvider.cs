using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;

public abstract class AbstractThirdPartyProvider : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AbstractThirdPartyProvider> _logger;

    protected readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    protected AbstractThirdPartyProvider(IHttpClientFactory httpClientFactory,
        ILogger<AbstractThirdPartyProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<T> Invoke<T>(string domain, ApiInfo apiInfo, 
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null)
    {
        var resp = await Invoke(domain, apiInfo, pathParams, param, body, header);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{apiInfo.Path}] response body: {resp}", ex);
        }
    }

    public async Task<string> Invoke(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null)
    {
        // url params
        var fullUrl = domain + apiInfo.PathParam(pathParams).Path;
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
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "Request To {FullUrl}, query={Query}, statusCode={StatusCode}, body={Body}, resp={Content}",
            fullUrl,  builder.Query, body, response.StatusCode, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{fullUrl}] returned status code {response.StatusCode} : {content}");
        }

        return content;
    }
}

public class ApiInfo
{
    public string Path { get; set; }
    public HttpMethod Method { get; set; }

    public ApiInfo(HttpMethod method, string path, string name = null)
    {
        Path = path;
        Method = method;
    }

    public ApiInfo PathParam(Dictionary<string, string> pathParams)
    {
        var newPath = pathParams.IsNullOrEmpty() ? Path 
            : pathParams.Aggregate(Path, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
        return new ApiInfo(Method, newPath);
    }
}