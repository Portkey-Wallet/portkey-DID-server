using System;
using System.Threading;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace CAServer.ContractEventHandler;

public class CAServerContractEventHandlerHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly IContractAppService _contractAppService;

    public CAServerContractEventHandlerHostedService(IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider, IContractAppService contractAppService)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _contractAppService = contractAppService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(_serviceProvider);
        await _contractAppService.InitializeIndexAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}