using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CAServer.Common.Dtos;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;


public interface IHttpProvider : ISingletonDependency
{
    Task<T> Invoke<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withLog = false);

    Task<string> Invoke(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withLog = false);

    Task<string> Invoke(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, bool withLog = false);

}

public class HttpProvider : IHttpProvider
{
    private static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
            .WithCamelCasePropertyNamesResolver()
            .IgnoreNullValue()
            .Build();
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpProvider> _logger;

    public HttpProvider(IHttpClientFactory httpClientFactory, ILogger<HttpProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<T> Invoke<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withLog = false)
    {
        var resp = await Invoke(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, withLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
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
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withLog = false)
    {
        return await Invoke(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, withLog);
    }
    
    public async Task<string> Invoke(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, bool withLog = false)
    {
        // url params
        var fullUrl = PathParamUrl(url, pathParams);
        
        var builder = new UriBuilder(fullUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var item in param ?? new Dictionary<string, string>())
            query[item.Key] = item.Value;
        builder.Query = query.ToString() ?? string.Empty;

        var request = new HttpRequestMessage(method, builder.ToString());

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
        
        // log
        
        _logger.LogDebug(
            "Request To {FullUrl}, query={Query}, header={Header}, statusCode={StatusCode}, body={Body}, resp={Content}",
            fullUrl, builder.Query, request.Headers.ToString(), response.StatusCode, body, content);
        if (withLog)
            _logger.LogInformation(
            "Request To {FullUrl}, query={Query}, statusCode={StatusCode}, body={Body}, resp={Content}",
            fullUrl, builder.Query, response.StatusCode, body, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{fullUrl}] returned status code {response.StatusCode} : {content}");
        }

        return content;
    }
    
    
    private static string PathParamUrl(string url, Dictionary<string, string> pathParams)
    {
        return pathParams.IsNullOrEmpty()
            ? url
            : pathParams.Aggregate(url, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
    }
}