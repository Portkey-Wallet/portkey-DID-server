using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Hub;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
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
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<HubService> _logger;
    private readonly IOrderWsNotifyProvider _orderWsNotifyProvider;

    private readonly Dictionary<string, string> _clientOrderListener = new();
    private readonly Dictionary<string, Func<NotifyOrderDto, Task>> _orderNotifyListeners = new();

    //private readonly IGrowthStatisticAppService _statisticAppService;


    public HubService(IHubProvider hubProvider, IHubCacheProvider hubCacheProvider, IHubProvider caHubProvider,
        IThirdPartOrderProvider thirdPartOrderProvider, IObjectMapper objectMapper,
        IConnectionProvider connectionProvider, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        ILogger<HubService> logger, IOrderWsNotifyProvider orderWsNotifyProvider)
    {
        _hubProvider = hubProvider;
        _hubCacheProvider = hubCacheProvider;
        _caHubProvider = caHubProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _connectionProvider = connectionProvider;
        _thirdPartOptions = thirdPartOptions;
        _logger = logger;
        _orderWsNotifyProvider = orderWsNotifyProvider;
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
        var clientId = _connectionProvider.Remove(connectionId);
        _orderWsNotifyProvider.UnRegisterOrderListenerAsync(clientId);
        return clientId;
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

    public async Task RequestRampOrderStatus(string clientId, string orderId)
    {
        await RequestOrderStatusAsync(clientId, orderId);
    }

    // public async Task ReferralRecordListAsync(ReferralRecordRequestDto input)
    // {
    //     // while (true)
    //     // {
    //     //     try
    //     //     {
    //     //         // stop while disconnected
    //     //         if (_connectionProvider.GetConnectionByClientId(input.TargetClientId) != null)
    //     //         {
    //     //             //await GetReferralRecordListAsync(input);
    //     //         }
    //     //         _logger.LogWarning("Get ReferralRecords STOP");
    //     //         break;
    //     //     }
    //     //     catch (Exception e)
    //     //     {
    //     //         _logger.LogError(e, "");
    //     //         break;
    //     //     }
    //     // }
    // }

    // public async Task RewardProgressAsync(ActivityEnums activityEnums, string targetClientId)
    // {
    //     // while (true)
    //     // {
    //     //     try
    //     //     {
    //     //         // stop while disconnected
    //     //         if (_connectionProvider.GetConnectionByClientId(targetClientId) != null)
    //     //         {
    //     //             //await RewardProgressChangedAsync(activityEnums, targetClientId);
    //     //         }
    //     //         _logger.LogWarning("Get RewardProgressChanged STOP");
    //     //         break;
    //     //     }
    //     //     catch (Exception e)
    //     //     {
    //     //         _logger.LogError(e, "");
    //     //         break;
    //     //     }
    //     //}
    // }
    //
    // private async Task RewardProgressChangedAsync(ActivityEnums activityEnums, string targetClientId)
    // {
    //     // var rewardProgressResponseDto = await _statisticAppService.GetRewardProgressAsync(activityEnums);
    //     // try
    //     // {
    //     //     var methodName = "RewardProgressChanged";
    //     //     await _caHubProvider.ResponseAsync(
    //     //         new HubResponseBase<RewardProgressResponseDto>(rewardProgressResponseDto), targetClientId,
    //     //         methodName);
    //     // }
    //     // catch (Exception e)
    //     // {
    //     //     _logger.LogError(e, "RewardProgressChanged error, clientId={ClientId}, enums={enums}", targetClientId,
    //     //         activityEnums.ToString());
    //     // }
    // }
    //
    // private async Task GetReferralRecordListAsync(ReferralRecordRequestDto dto)
    // {
    //     // var referralRecordResponseDto = await _statisticAppService.GetReferralRecordList(dto);
    //     // try
    //     // {
    //     //     var methodName = "ReferralRecordListChanged";
    //     //     await _caHubProvider.ResponseAsync(
    //     //         new HubResponseBase<ReferralRecordResponseDto>(referralRecordResponseDto), dto.TargetClientId,
    //     //         methodName);
    //     // }
    //     // catch (Exception e)
    //     // {
    //     //     _logger.LogError(e, "Get ReferralRecordList error, clientId={ClientId}, dto={dto}", dto.TargetClientId,
    //     //         JsonConvert.SerializeObject(dto));
    //     // }
    // }


    public async Task RequestNFTOrderStatusAsync(string clientId, string orderId)
    {
        await RequestOrderStatusAsync(clientId, orderId);
    }

    public async Task RequestOrderStatusAsync(string clientId, string orderId)
    {
        await _orderWsNotifyProvider.RegisterOrderListenerAsync(clientId, orderId, async notifyOrderDto =>
        {
            try
            {
                var methodName = notifyOrderDto.IsNftOrder() ? "OnNFTOrderChanged" : "OnRampOrderChanged";
                await _caHubProvider.ResponseAsync(new HubResponseBase<NotifyOrderDto>(notifyOrderDto), clientId,
                    methodName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "notify orderStatus error, clientId={ClientId}, orderId={OrderId}", clientId,
                    orderId);
            }
        });

        // notify current order immediately
        var currentOrder = await _thirdPartOrderProvider.GetThirdPartOrderIndexAsync(orderId);
        var notifyOrderDto = _objectMapper.Map<RampOrderIndex, NotifyOrderDto>(currentOrder);
        await _orderWsNotifyProvider.NotifyOrderDataAsync(notifyOrderDto);
    }

    public async Task RequestOrderTransferredAsync(string targetClientId, string orderId)
    {
        await RequestConditionOrderAsync(targetClientId, orderId,
            esOrderData => esOrderData.Status == OrderStatusType.Transferred.ToString()
                           || esOrderData.Status == OrderStatusType.TransferFailed.ToString()
                           || esOrderData.Status == OrderStatusType.Invalid.ToString(),
            "onOrderTransferredReceived");
    }

    public async Task RequestAchTxAddressAsync(string targetClientId, string orderId)
    {
        await RequestConditionOrderAsync(targetClientId, orderId,
            esOrderData => !string.IsNullOrWhiteSpace(esOrderData.Address),
            "onAchTxAddressReceived");
    }


    private async Task RequestConditionOrderAsync(string targetClientId, string orderId,
        Func<OrderDto, bool> matchCondition, string callbackMethod)
    {
        var cts = new CancellationTokenSource(_thirdPartOptions.CurrentValue.Timer.TimeoutMillis);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                // stop while disconnected
                if (_connectionProvider.GetConnectionByClientId(targetClientId) == null)
                {
                    _logger.LogWarning(
                        "Get third-part order {OrderId} {CallbackMethod} STOP, connection disconnected",
                        orderId, callbackMethod);
                    break;
                }

                var grainId = ThirdPartHelper.GetOrderId(orderId);
                var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
                if (esOrderData == null || esOrderData.Id == new Guid())
                {
                    _logger.LogError("This order {OrderId} {CallbackMethod} not exists in the es", orderId,
                        callbackMethod);
                    break;
                }

                // condition mot match
                if (!matchCondition(esOrderData))
                {
                    _logger.LogWarning(
                        "Get third-part order {OrderId} {CallbackMethod} condition not match, wait for next time",
                        orderId, callbackMethod);
                    await Task.Delay(TimeSpan.FromSeconds(_thirdPartOptions.CurrentValue.Timer.DelaySeconds));
                    continue;
                }

                // push address to client via ws
                var bodyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(
                        new NotifyOrderDto()
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
                _logger.LogError(oce,
                    "Timed out waiting for third-part order { {OrderId} {CallbackMethod}  update status", orderId,
                    callbackMethod);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "An exception occurred during the query third-part order {OrderId} {CallbackMethod} ",
                    orderId, callbackMethod);
                break;
            }
        }

        cts.Cancel();
    }
}