using System;
using System.Threading;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace CAServer.RedPackage;

public class CAServerRedPackageHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;

    public CAServerRedPackageHostedService(IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(_serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}