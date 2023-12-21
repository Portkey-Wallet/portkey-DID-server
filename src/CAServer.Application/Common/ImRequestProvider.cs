using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Monitor;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public class ImRequestProvider : IImRequestProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ImServerOptions _imServerOptions;
    private readonly ILogger<ImRequestProvider> _logger;
    private readonly IIndicatorScope _indicatorScope;

    public ImRequestProvider(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor,
        IOptionsSnapshot<ImServerOptions> imServerOptions, ILogger<ImRequestProvider> logger,
        IIndicatorScope indicatorScope)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _imServerOptions = imServerOptions.Value;
        _logger = logger;
        _indicatorScope = indicatorScope;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        url = GetUrl(url);
        var interIndicator = _indicatorScope.Begin(MonitorTag.Http);

        var client = GetClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        _indicatorScope.End(MonitorHelper.GetRequestUrl(response), interIndicator);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, url:{url}",
                response.StatusCode, content, url);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return GetData(JsonConvert.DeserializeObject<ImResponseDto<T>>(content));
    }

    public async Task<T> GetAsync<T>(string url, IDictionary<string, string> headers)
    {
        url = GetUrl(url);
        var interIndicator = _indicatorScope.Begin(MonitorTag.Http);

        if (headers == null)
        {
            return await GetAsync<T>(url);
        }

        var client = GetClient();
        foreach (var keyValuePair in headers)
        {
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        _indicatorScope.End(MonitorHelper.GetRequestUrl(response), interIndicator);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, url:{url}",
                response.StatusCode, content, url);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return GetData(JsonConvert.DeserializeObject<ImResponseDto<T>>(content));
    }

    public async Task<T> PostAsync<T>(string url)
    {
        var response =
            await PostJsonAsync<ImResponseDto<T>>(url, null, null);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj)
    {
        var response =
            await PostJsonAsync<ImResponseDto<T>>(url, paramObj, null);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        var response =
            await PostJsonAsync<ImResponseDto<T>>(url, paramObj, headers);

        return GetData(response);
    }

    public async Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
        Dictionary<string, string> headers)
    {
        if (requestMediaType == RequestMediaType.Json)
        {
            var jsonResponse = await PostJsonAsync<ImResponseDto<T>>(url, paramObj, headers);
            return GetData(jsonResponse);
        }

        var response = await PostFormAsync<ImResponseDto<T>>(url, (Dictionary<string, string>)paramObj, headers);

        return GetData(response);
    }

    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        url = GetUrl(url);
        var interIndicator = _indicatorScope.Begin(MonitorTag.Http);

        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var requestInput = paramObj == null
            ? string.Empty
            : JsonConvert.SerializeObject(paramObj, Formatting.None, serializerSettings);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = GetClient();

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();
        _indicatorScope.End(MonitorHelper.GetRequestUrl(response), interIndicator);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> paramDic,
        Dictionary<string, string> headers)
    {
        url = GetUrl(url);
        var interIndicator = _indicatorScope.Begin(MonitorTag.Http);

        var client = GetClient();
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
        _indicatorScope.End(MonitorHelper.GetRequestUrl(response), interIndicator);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Response status code not good, code:{code}, message: {message}, params:{param}",
                response.StatusCode, content, JsonConvert.SerializeObject(paramDic));

            throw new Exception("");
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private T GetData<T>(ImResponseDto<T> response)
    {
        if (response.Code != ImConstant.SuccessCode)
        {
            throw new UserFriendlyException(response.Message, response.Code);
        }

        return response.Data;
    }

    private HttpClient GetClient()
    {
        var authToken = _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.AuthHeader]
            .FirstOrDefault();

        var imAuthToken = _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.ImAuthHeader]
            .FirstOrDefault();

        if (imAuthToken.IsNullOrWhiteSpace() || authToken.IsNullOrWhiteSpace())
        {
            throw new Exception();
        }

        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Add(CommonConstant.ImAuthHeader, imAuthToken);

        client.DefaultRequestHeaders.Add(CommonConstant.AuthHeader, authToken);

        return client;
    }

    private string GetUrl(string url)
    {
        if (_imServerOptions == null || _imServerOptions.BaseUrl.IsNullOrWhiteSpace())
        {
            return url;
        }

        return $"{_imServerOptions.BaseUrl.TrimEnd('/')}/{url}";
    }
}