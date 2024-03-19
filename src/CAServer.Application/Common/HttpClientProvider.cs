using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public class HttpClientProvider : IHttpClientProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpClientProvider> _logger;

    public HttpClientProvider(IHttpClientFactory httpClientFactory, ILogger<HttpClientProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null,
        Dictionary<string, string> parameters = null, int timeout = 0)
    {
        var client = _httpClientFactory.CreateClient();
        if (headers != null)
        {
            foreach (var keyValuePair in headers)
            {
                client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        if (timeout > 0)
        {
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
        }

        var paramStringBuilder = new StringBuilder(url);
        if (parameters is { Count: > 0 })
        {
            paramStringBuilder.Append('?');
            foreach (var parameter in parameters)
            {
                paramStringBuilder.Append(parameter.Key).Append('=').Append(parameter.Value).Append('&');
            }

            paramStringBuilder.Length -= 1;
            url = paramStringBuilder.ToString();
        }

        var response = await client.GetStringAsync(url);
        return JsonConvert.DeserializeObject<T>(response);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        return await PostJsonAsync<T>(url, paramObj, headers);
    }

    private async Task<T> PostJsonAsync<T>(string url, object paramObj, Dictionary<string, string> headers)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(HttpConstant.RetryHttpClient);

        if (headers is { Count: > 0 })
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
}