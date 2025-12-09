using CAServer.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hub;

public interface IHubCacheProvider
{
    Task SetResponseAsync<T>(HubResponseCacheEntity<T> res, string clientId);
    Task<List<HubResponseCacheEntity<object>>> GetResponseByClientId(string clientId);
    Task<HubResponseCacheEntity<object>> GetRequestById(string requestId);

    Task RemoveResponseByClientId(string clientId, string requestId);
}

public class HubCacheProvider : IHubCacheProvider, ISingletonDependency
{
    private readonly ICacheProvider _cacheProvider;
    private readonly HubCacheOptions _hubCacheOptions;
    private readonly ILogger<HubCacheProvider> _logger;

    public HubCacheProvider(ICacheProvider cacheProvider, IOptions<HubCacheOptions> hubCacheOptions,
        ILogger<HubCacheProvider> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
        _hubCacheOptions = hubCacheOptions.Value;
    }


    public async Task SetResponseAsync<T>(HubResponseCacheEntity<T> res, string clientId)
    {
        var resJsonStr = Serialize(res);
        var requestCacheKey = MakeResponseCacheKey(res.Response.RequestId);
        var clientCacheKey = MakeClientCacheKey(clientId);
        await _cacheProvider.Set(requestCacheKey, resJsonStr, GetMethodResponseTtl(res.Method));
        _logger.LogDebug("set cache={requestCacheKey} body={body}", requestCacheKey, resJsonStr);
        _cacheProvider.HSetWithExpire(clientCacheKey, res.Response.RequestId, "", GetClientCacheTtl());
        _logger.LogInformation(
            "set requestCacheKey={requestCacheKey}, clientCacheKey={clientCacheKey}, requestId={RequestId}",
            requestCacheKey, clientCacheKey, res.Response.RequestId);
    }

    public async Task<List<HubResponseCacheEntity<object>>> GetResponseByClientId(string clientId)
    {
        var requestIds = await _cacheProvider.HGetAll(MakeClientCacheKey(clientId));
        var ans = new List<HubResponseCacheEntity<object>>();
        if (requestIds == null || requestIds.Length == 0)
        {
            return ans;
        }

        var responseKeys = requestIds.Select(requestId => MakeResponseCacheKey(requestId.Name)).ToList();
        var ansValues = await _cacheProvider.BatchGet(responseKeys);

        ans.AddRange(ansValues.Select(kv => Deserialize<HubResponseCacheEntity<object>>(kv.Value)));
        return ans;
    }

    private string Serialize(object val)
    {
        var serializeSetting = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return JsonConvert.SerializeObject(val, Formatting.None, serializeSetting);
    }

    private T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task<HubResponseCacheEntity<object>> GetRequestById(string requestId)
    {
        string jsonStr = await _cacheProvider.Get(MakeResponseCacheKey(requestId));
        _logger.LogDebug("set cache={requestCacheKey} body={body}", MakeResponseCacheKey(requestId), jsonStr);
        return jsonStr == null ? null : Deserialize<HubResponseCacheEntity<object>>(jsonStr);
    }

    public async Task RemoveResponseByClientId(string clientId, string requestId)
    {
        var requestCacheKey = MakeResponseCacheKey(requestId);
        var clientCacheKey = MakeClientCacheKey(clientId);
        await _cacheProvider.HashDeleteAsync(clientCacheKey, requestId);
        await _cacheProvider.Delete(requestCacheKey);
    }

    private string MakeResponseCacheKey(string requestId)
    {
        return $"hub_req_cache:{requestId}";
    }

    private string MakeClientCacheKey(string clientId)
    {
        return $"hub_cli_cache:{clientId}";
    }

    private TimeSpan GetMethodResponseTtl(string method)
    {
        return _hubCacheOptions.MethodResponseTtl.TryGetValue(method, out var value)
            ? new TimeSpan(0, 0, value)
            : new TimeSpan(0, 0, _hubCacheOptions.DefaultResponseTtl);
    }

    private TimeSpan GetClientCacheTtl()
    {
        var max = _hubCacheOptions.MethodResponseTtl.Select(kv => kv.Value).Prepend(_hubCacheOptions.DefaultResponseTtl)
            .Max();
        return new TimeSpan(0, 0, max);
    }
}

public class HubResponseCacheEntity<T>
{
    public HubResponseCacheEntity()
    {
    }

    public HubResponseCacheEntity(T body, string requestId, string method, Type type)
    {
        Response = new HubResponse<T>() { RequestId = requestId, Body = body };
        Method = method;
        Type = type;
    }

    public HubResponse<T> Response { get; set; }
    public string Method { get; set; }
    public Type Type { get; set; }
}

public class HubResponse<T>
{
    public string RequestId { get; set; }
    public T Body { get; set; }
}