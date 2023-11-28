using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.RedPackage;
using CAServer.RedPackage.Etos;
using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;


namespace CAServer.ContractEventHandler.Core;

public class RedPackageEventHandler : IDistributedEventHandler<RedPackageCreateEto>, ITransientDependency
{
    private readonly ILogger<RedPackageEventHandler> _logger;
    private readonly IContractAppService _contractAppService;
    private readonly IDistributedEventBus _distributedEventBus;

    public RedPackageEventHandler(IContractAppService contractAppService, IDistributedEventBus distributedEventBus,
        ILogger<RedPackageEventHandler> logger)
    {
        _contractAppService = contractAppService;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
    }

    public async Task HandleEventAsync(RedPackageCreateEto eventData)
    {
        var eto = new RedPackageCreateResultEto();
        eto.SessionId = eventData.SessionId;
        try
        {
            var result = await _contractAppService.CreateRedPackageAsync(eventData);
            _logger.LogInformation("RedPackageCreate result: " + "\n{result}",
                JsonConvert.SerializeObject(result, Formatting.Indented));
            eto.TransactionResult = result.Status;
            eto.TransactionId = result.TransactionId;
            if (result.Status != TransactionState.Mined)
            {
                eto.Message = "Transaction status: " + result.Status + ". Error: " +
                              result.Error;
                eto.Success = false;

                _logger.LogInformation("RedPackageCreate pushed: " + "\n{result}",
                    JsonConvert.SerializeObject(eto, Formatting.Indented));

                await _distributedEventBus.PublishAsync(eto);
                return;
            }
            
            if (!result.Logs.Select(l => l.Name).Contains(LogEvent.RedPacketCreated))
            {
                eto.Message = "Transaction status: FAILED" + ". Error: Verification failed";
                eto.Success = false;

                _logger.LogInformation("RedPackageCreate pushed: " + "\n{result}",
                    JsonConvert.SerializeObject(eto, Formatting.Indented));

                await _distributedEventBus.PublishAsync(eto);
                return;
            }
            //TODO daiyabin  how long would i excute it
            //
            // BackgroundJob.Schedule<PayRedPackageTask>(x => x.PayRedPackageAsync(eventData));
            eto.Success = true;
            eto.Message = "Transaction status: " + result.Status;
            await _distributedEventBus.PublishAsync(eto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "RedPackageCreateEto Error: user:{user},sessionId:{session}", eventData.UserId,
                eventData.SessionId);
            eto.Success = false;
            eto.Message = e.Message;
            await _distributedEventBus.PublishAsync(eto);
        }
    }
}