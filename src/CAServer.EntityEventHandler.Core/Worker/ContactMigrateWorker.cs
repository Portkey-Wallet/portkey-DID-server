using System.Threading.Tasks;
using CAServer.AddressBook.Migrate;
using CAServer.Options;
using CAServer.ScheduledTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.EntityEventHandler.Core.Worker;

public class ContactMigrateWorker : ScheduledTaskBase
{
    private readonly IOptionsMonitor<ContactMigrateOptions> _options;
    private readonly IAddressBookMigrateService _service;
    private readonly ILogger<ContactMigrateWorker> _logger;

    public ContactMigrateWorker(
        IOptionsMonitor<ContactMigrateOptions> options, IAddressBookMigrateService service,
        ILogger<ContactMigrateWorker> logger)
    {
        _options = options;
        _service = service;
        _logger = logger;
        Period = options.CurrentValue.Period * 1000;
    }

    protected override async Task DoWorkAsync()
    {
        _logger.LogInformation("[AddressBookMigrate] ContactMigrateWorker start.");
        if (!_options.CurrentValue.Open)
        {
            _logger.LogInformation("[AddressBookMigrate] migrate not open.");
            return;
        }

        await _service.MigrateAsync();
        _logger.LogInformation("[AddressBookMigrate] ContactMigrateWorker end.");
    }
}