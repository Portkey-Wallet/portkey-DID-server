using System;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Message.Etos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Alchemy;

public class AlchemyBridgeHandler : IDistributedEventHandler<GetAlchemyTargetAddressEto>, ITransientDependency
{
    private readonly IHubProvider _caHubProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ILogger<AlchemyBridgeHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly TimeSpan _delayTimeout = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _cancelTimeout = TimeSpan.FromMinutes(5);


    public AlchemyBridgeHandler(IHubProvider caHubProvider, IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<AlchemyBridgeHandler> logger, IObjectMapper objectMapper)
    {
        _logger = logger;
        _caHubProvider = caHubProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(GetAlchemyTargetAddressEto eventData)
    {
        CancellationTokenSource cts = new CancellationTokenSource(_cancelTimeout);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                Guid grainId = ThirdPartHelper.GetOrderId(eventData.OrderId);
                var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
                if (esOrderData == null || eventData.OrderId != esOrderData.Id.ToString())
                {
                    _logger.LogError("This order {OrderId} not exists in the es.", eventData.OrderId);
                    break;
                }

                if (!string.IsNullOrWhiteSpace(esOrderData.Address))
                {
                    await _caHubProvider.ResponseAsync(
                        new HubResponseBase<string>
                        {
                            Body = JsonConvert.SerializeObject(
                                _objectMapper.Map<OrderDto, AlchemyTargetAddressDto>(esOrderData))
                        },
                        eventData.TargetClientId, "returnAlchemyTargetAddress");
                    _logger.LogInformation("Get alchemy order {orderId} target address {address} success",
                        eventData.OrderId, esOrderData.Address);
                    break;
                }
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogError(oce, "Timed out waiting for alchemy order {orderId} update status",
                    eventData.OrderId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred during the query alchemy order {orderId} target address.",
                    eventData.OrderId);
                break;
            }

            _logger.LogWarning("Get alchemy order {orderId} target address failed, wait for next time.",
                eventData.OrderId);
            await Task.Delay(_delayTimeout);
        }

        cts.Cancel();
    }
}