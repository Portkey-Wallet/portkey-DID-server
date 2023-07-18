using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Hub;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace CAServer.Hubs;

[RemoteService(false)]
[DisableAuditing]
public class HubService : CAServerAppService, IHubService
{
    private readonly IHubProvider _hubProvider;
    private readonly IHubProvider _caHubProvider;
    private readonly IHubCacheProvider _hubCacheProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<HubService> _logger;

    public HubService(IHubProvider hubProvider, IHubCacheProvider hubCacheProvider, IHubProvider caHubProvider,
        IThirdPartOrderProvider thirdPartOrderProvider, IObjectMapper objectMapper,
        IConnectionProvider connectionProvider, IOptions<ThirdPartOptions> merchantOptions,
        ILogger<HubService> logger)
    {
        _hubProvider = hubProvider;
        _hubCacheProvider = hubCacheProvider;
        _caHubProvider = caHubProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _connectionProvider = connectionProvider;
        _thirdPartOptions = merchantOptions.Value;
        _logger = logger;
    }

     public async Task Ping(HubRequestContext context, string content)
    {
        const string PingMethodName = "Ping";
        _hubProvider.ResponseAsync(new HubResponse<string>() { RequestId = context.RequestId, Body = content },
            context.ClientId, PingMethodName);
    }

    public async Task<HubResponse<object>> GetResponse(HubRequestContext context)
    {
        var cacheRes = await _hubCacheProvider.GetRequestById(context.RequestId);
        if (cacheRes == null)
        {
            return null;
        }

        _hubCacheProvider.RemoveResponseByClientId(context.ClientId, context.RequestId);
        return new HubResponse<object>()
        {
            RequestId = cacheRes.Response.RequestId,
            Body = cacheRes.Response.Body,
        };
    }

    public async Task RegisterClient(string clientId, string connectionId)
    {
        _connectionProvider.Add(clientId, connectionId);
    }

    public string UnRegisterClient(string connectionId)
    {
        return _connectionProvider.Remove(connectionId);
    }

    public async Task SendAllUnreadRes(string clientId)
    {
        var unreadRes = await _hubCacheProvider.GetResponseByClientId(clientId);
        if (unreadRes == null || unreadRes.Count == 0)
        {
            _logger.LogInformation("clientId={clientId}'s unread res is null", clientId);
            return;
        }

        foreach (var res in unreadRes)
        {
            try
            {
                await _hubProvider.ResponseAsync(new HubResponse<object>()
                {
                    RequestId = res.Response.RequestId, Body = res.Response.Body
                }, clientId, res.Method, res.Type);
                _logger.LogInformation("syncOnConnect requestId={requestId} to clientId={clientId} method={method}",
                    res.Response.RequestId, clientId, res.Method);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "syncOnConnect failed requestId={requestId} to clientId={clientId} method={method}, exception={message}",
                    res.Response.RequestId, clientId, res.Method, e.Message);
            }
        }
    }

    public async Task Ack(string clientId, string requestId)
    {
        await _hubCacheProvider.RemoveResponseByClientId(clientId, requestId);
    }
    
    public async Task RequestOrderTransferredAsync(string targetClientId, string orderId)
    {
        await RequestConditionOrderAsync(targetClientId, orderId,      
            esOrderData => esOrderData.Status == OrderStatusType.Transferred.ToString() 
                           || esOrderData.Status == OrderStatusType.TransferFailed.ToString(),
            "onOrderTransferredReceived");
    }

    public async Task RequestAchTxAddressAsync(string targetClientId, string orderId)
    {
        await RequestConditionOrderAsync(targetClientId, orderId, 
            esOrderData => !string.IsNullOrWhiteSpace(esOrderData.Address),
            "onAchTxAddressReceived");
    }
    

    private async Task RequestConditionOrderAsync(string targetClientId, string orderId, Func<OrderDto, bool> matchCondition, string callbackMethod)
    {
        var cts = new CancellationTokenSource(_thirdPartOptions.timer.TimeoutMillis);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                // stop while disconnected
                if (_connectionProvider.GetConnectionByClientId(targetClientId) == null)
                {
                    _logger.LogWarning("Get third-part order {OrderId} {CallbackMethod} STOP, connection disconnected",
                        orderId, callbackMethod);
                    break;
                }

                var grainId = ThirdPartHelper.GetOrderId(orderId);
                var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
                if (esOrderData == null || esOrderData.Id == new Guid())
                {
                    _logger.LogError("This order {OrderId} {CallbackMethod} not exists in the es", orderId, callbackMethod);
                    break;
                }

                // condition mot match
                if (!matchCondition(esOrderData))
                {
                    _logger.LogWarning("Get third-part order {OrderId} {CallbackMethod} condition not match, wait for next time",
                        orderId, callbackMethod);
                    await Task.Delay(TimeSpan.FromSeconds(_thirdPartOptions.timer.DelaySeconds));
                    continue;
                }

                // push address to client via ws
                var bodyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(
                    new AlchemyTargetAddressDto()
                    {
                        OrderId = esOrderData.Id,
                        MerchantName = esOrderData.MerchantName,
                        Address = esOrderData.Address,
                        Network = esOrderData.Network,
                        Crypto = esOrderData.Crypto,
                        CryptoAmount = esOrderData.CryptoAmount,
                        Status = esOrderData.Status
                    },
                    Formatting.None,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));
                await _caHubProvider.ResponseAsync(
                    new HubResponseBase<Dictionary<string, string>>
                    {
                        Body = bodyDict
                    },
                    targetClientId, callbackMethod
                );
                _logger.LogInformation("Get third-part order {OrderId} {CallbackMethod}  success",
                    orderId, callbackMethod);
                break;
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogError(oce, "Timed out waiting for third-part order { {OrderId} {CallbackMethod}  update status", orderId, callbackMethod);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred during the query third-part order {OrderId} {CallbackMethod} ",
                    orderId, callbackMethod);
                break;
            }
        }

        cts.Cancel();
    }
}