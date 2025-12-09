using System;
using System.Threading.Tasks;
using CAServer.DataReporting;
using CAServer.DataReporting.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.HubsEventHandler;

public class DataReportHandler : IDistributedEventHandler<ExitWalletEto>, ITransientDependency
{
    private readonly IDeviceInfoReportAppService _deviceInfoReportAppService;
    private readonly ILogger<DataReportHandler> _logger;

    public DataReportHandler(IDeviceInfoReportAppService deviceInfoReportAppService, ILogger<DataReportHandler> logger)
    {
        _deviceInfoReportAppService = deviceInfoReportAppService;
        _logger = logger;
    }

    public async Task HandleEventAsync(ExitWalletEto eventData)
    {
        try
        {
            if (eventData.DeviceId.IsNullOrEmpty())
            {
                _logger.LogWarning("exist wallet, deviceId is empty, userId:{userId}",
                    eventData.UserId);
                return;
            }

            await _deviceInfoReportAppService.ExitWalletAsync(eventData.DeviceId, eventData.UserId);
            _logger.LogWarning("exist wallet success, deviceId:{deviceId},userId:{userId}",
                eventData.DeviceId, eventData.UserId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle exist wallet error, deviceId:{deviceId},userId:{userId}",
                eventData.DeviceId ?? "-",
                eventData.UserId);
        }
    }
}