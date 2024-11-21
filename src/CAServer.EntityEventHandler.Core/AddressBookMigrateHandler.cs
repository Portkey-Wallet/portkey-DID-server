using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AddressBook.Migrate.Eto;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class AddressBookMigrateHandler : IDistributedEventHandler<AddressBookMigrateEto>, ITransientDependency
{
    private readonly INESTRepository<AddressBookMigrateIndex, string> _addressBookRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AddressBookMigrateHandler> _logger;

    public AddressBookMigrateHandler(INESTRepository<AddressBookMigrateIndex, string> addressBookRepository,
        IObjectMapper objectMapper, ILogger<AddressBookMigrateHandler> logger)
    {
        _addressBookRepository = addressBookRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(AddressBookMigrateEto eventData)
    {
        try
        {
            var addressBook = _objectMapper.Map<AddressBookMigrateEto, AddressBookMigrateIndex>(eventData);
            await _addressBookRepository.AddOrUpdateAsync(addressBook);
            _logger.LogInformation(
                "[AddressBookMigrate] add or update success, userId:{0}, originalContactId:{1}",
                eventData.UserId.ToString(), eventData.OriginalContactId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AddressBookMigrate] handler error, {data}", JsonConvert.SerializeObject(eventData));
        }
    }
}