using System.Threading.Tasks;
using CAServer.AddressBook.Migrate;
using CAServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ContactMigrateWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IOptionsMonitor<ContactMigrateOptions> _options;
    private readonly IAddressBookMigrateService _service;
    private readonly ILogger<ContactMigrateWorker> _logger;

    public ContactMigrateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<ContactMigrateOptions> options, IAddressBookMigrateService service,
        ILogger<ContactMigrateWorker> logger) : base(timer,
        serviceScopeFactory)
    {
        _options = options;
        _service = service;
        _logger = logger;
        timer.RunOnStart = true;
        timer.Period = options.CurrentValue.Period * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("[AddressBookMigrate] ContactMigrateWorker start.");
        if (!_options.CurrentValue.Open)
        {
            //lob
            _logger.LogInformation("[AddressBookMigrate] migrate not open.");
            return;
        }

        await _service.MigrateAsync();
        _logger.LogInformation("[AddressBookMigrate] ContactMigrateWorker end.");
    }
}