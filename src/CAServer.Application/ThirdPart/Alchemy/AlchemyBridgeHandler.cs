using System;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Message.Etos;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Alchemy;

public class AlchemyBridgeHandler : IDistributedEventHandler<AlchemyTargetAddressEto>, ITransientDependency
{
    private readonly IHubProvider _caHubProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ILogger<AlchemyBridgeHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly ThirdPartOptions _thirdPartOptions;


    public AlchemyBridgeHandler(IHubProvider caHubProvider, IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<AlchemyBridgeHandler> logger, IObjectMapper objectMapper, IOptions<ThirdPartOptions> merchantOptions)
    {
        _logger = logger;
        _caHubProvider = caHubProvider;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _thirdPartOptions = merchantOptions.Value;
    }

    public async Task HandleEventAsync(AlchemyTargetAddressEto eventData)
    {
        CancellationTokenSource cts = new CancellationTokenSource(_thirdPartOptions.timer.Timeout);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                Guid grainId = ThirdPartHelper.GetOrderId(eventData.OrderId);
                var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
                if (esOrderData == null)
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
                        eventData.TargetClientId, "returnAlchemyTargetAddress"
                    );
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
            await Task.Delay(TimeSpan.FromSeconds(_thirdPartOptions.timer.Delay));
        }

        cts.Cancel();
    }
}