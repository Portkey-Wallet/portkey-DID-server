using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AddressBook.Etos;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class AddressBookHandler : IDistributedEventHandler<AddressBookEto>, ITransientDependency
{
    private readonly INESTRepository<AddressBookIndex, Guid> _addressBookRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AddressBookHandler> _logger;

    public AddressBookHandler(INESTRepository<AddressBookIndex, Guid> addressBookRepository,
        IObjectMapper objectMapper,
        ILogger<AddressBookHandler> logger)
    {
        _addressBookRepository = addressBookRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(AddressBookEto eventData)
    {
        try
        {
            var addressBook = _objectMapper.Map<AddressBookEto, AddressBookIndex>(eventData);
            await _addressBookRepository.AddOrUpdateAsync(addressBook);
            _logger.LogInformation(
                "address book add or update success, userId:{userId}, address:{address}, name:{name}, isDeleted:{isDeleted}",
                eventData.UserId.ToString(), eventData.AddressInfo.Address, eventData.Name, eventData.IsDeleted);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "address book add error, {data}", JsonConvert.SerializeObject(eventData));
        }
    }
}