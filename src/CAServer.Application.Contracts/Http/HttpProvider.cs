using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CAServer.Commons;
using CAServer.Http.Dtos;
using CAServer.Monitor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Http;

public interface IHttpProvider : ISingletonDependency
{
    Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true);

    Task<string> InvokeAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true);

    Task<T> InvokeAsync<T>(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true);

    Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, bool withInfoLog = false, bool withDebugLog = true);

    Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        bool withInfoLog = false, bool withDebugLog = true);

    Task<HttpResponseMessage> InvokeResponseAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true);
}

public class HttpProvider : IHttpProvider
{
    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIndicatorScope _indicatorScope;
    private readonly ILogger<HttpProvider> _logger;

    public HttpProvider(IHttpClientFactory httpClientFactory, ILogger<HttpProvider> logger,
        IIndicatorScope indicatorScope)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _indicatorScope = indicatorScope;
    }

    public async Task<T> InvokeAsync<T>(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true)
    {
        var resp = await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header,
            withInfoLog, withDebugLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{apiInfo.Path}] response body: {resp}", ex);
        }
    }

    public async Task<HttpResponseMessage> InvokeResponseAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true)
    {
        return await InvokeResponseAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header,
            withInfoLog, withDebugLog);
    }

    public async Task<string> InvokeAsync(string domain, ApiInfo apiInfo,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null, JsonSerializerSettings settings = null, bool withInfoLog = false,
        bool withDebugLog = true)
    {
        return await InvokeAsync(apiInfo.Method, domain + apiInfo.Path, pathParams, param, body, header, withInfoLog,
            withDebugLog);
    }

    public async Task<T> InvokeAsync<T>(HttpMethod method, string url, Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null, Dictionary<string, string> header = null, JsonSerializerSettings settings = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var resp = await InvokeAsync(method, url, pathParams, param, body, header, withInfoLog, withDebugLog);
        try
        {
            return JsonConvert.DeserializeObject<T>(resp, settings ?? DefaultJsonSettings);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error deserializing service [{url}] response body: {resp}", ex);
        }
    }

    public async Task<string> InvokeAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var response =
            await InvokeResponseAsync(method, url, pathParams, param, body, header, withInfoLog, withDebugLog);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Server [{url}] returned status code {response.StatusCode} : {content},param:{JsonConvert.SerializeObject(param).ToString()}",
                null, response.StatusCode);
        }

        return content;
    }

    public async Task<HttpResponseMessage> InvokeResponseAsync(HttpMethod method, string url,
        Dictionary<string, string> pathParams = null,
        Dictionary<string, string> param = null,
        string body = null,
        Dictionary<string, string> header = null,
        bool withInfoLog = false, bool withDebugLog = true)
    {
        var interIndicator = _indicatorScope.Begin(MonitorTag.Http);
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
        var stopwatch = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var time = stopwatch.ElapsedMilliseconds;
        // log
        // if (withInfoLog)
        //     _logger.LogInformation(
        //         "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, body={Body}, resp={Content}",
        //         fullUrl, response.StatusCode, time, builder.Query, body, content);
        // else if (withDebugLog)
        //     _logger.LogDebug(
        //         "Request To {FullUrl}, statusCode={StatusCode}, time={Time}, query={Query}, body={Body}, resp={Content}",
        //         fullUrl, response.StatusCode, time, builder.Query, body, content);
        // else
        //     _logger.LogDebug(
        //         "Request To {FullUrl}, statusCode={StatusCode}, time={Time}",
        //         fullUrl, response.StatusCode, time);
        
        _indicatorScope.End(MonitorHelper.GetRequestUrl(response), interIndicator);
        return response;
    }


    private static string PathParamUrl(string url, Dictionary<string, string> pathParams)
    {
        return pathParams.IsNullOrEmpty()
            ? url
            : pathParams.Aggregate(url, (current, param) => current.Replace($"{{{param.Key}}}", param.Value));
    }
}