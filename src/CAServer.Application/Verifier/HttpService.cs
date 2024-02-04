using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace CAServer.Verifier;


[Obsolete("This interface is deprecated.")]
public interface IHttpService
{
    Task<T?> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
        string? version = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK,
        AuthenticationHeaderValue? authenticationHeaderValue = null);
}

[Obsolete("This class is deprecated.")]
public class HttpService : IHttpService
{
    private readonly bool _useCamelCase;
    private HttpClient? Client { get; set; }
    private int TimeoutSeconds { get; }
    private readonly IHttpClientFactory _httpClientFactory;
    
    public HttpService(int timeoutSeconds, IHttpClientFactory httpClientFactory, bool useCamelCase = false)
    {
        _useCamelCase = useCamelCase;
        TimeoutSeconds = timeoutSeconds;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T?> PostResponseAsync<T>(string url, Dictionary<string, string> parameters,
        string? version = null,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK,
        AuthenticationHeaderValue? authenticationHeaderValue = null)
    {
        var response = await PostResponseAsync(url, parameters, version, true, expectedStatusCode,
            authenticationHeaderValue);
        var stream = await response.Content.ReadAsStreamAsync();
        var jsonSerializerOptions = _useCamelCase
            ? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
            : new JsonSerializerOptions();
        return await JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions);
    }
    
    private HttpClient GetHttpClient(string? version = null)
    {
        if (Client != null)
        {
            return Client;
        }

        Client = _httpClientFactory.CreateClient();
        Client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
        Client.DefaultRequestHeaders.Add("Connection", "close");
        return Client;

    }
    
    private async Task<HttpResponseMessage> PostResponseAsync(string url,
        Dictionary<string, string> parameters,
        string? version = null, bool useApplicationJson = false,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK,
        AuthenticationHeaderValue? authenticationHeaderValue = null)
    {
        version = !string.IsNullOrWhiteSpace(version) ? $";v={version}" : string.Empty;
        var client = GetHttpClient(version);

        if (authenticationHeaderValue != null)
        {
            client.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
        }

        HttpContent content;
        if (useApplicationJson)
        {
            var paramsStr = JsonSerializer.Serialize(parameters);
            content = new StringContent(paramsStr, Encoding.UTF8, "application/json");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json{version}");
        }

        else
        {
            content = new FormUrlEncodedContent(parameters);
            content.Headers.ContentType =
                MediaTypeHeaderValue.Parse($"application/x-www-form-urlencoded{version}");
        }

        try
        {
            var response = await client.PostAsync(url, content);
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
       
    }
}

