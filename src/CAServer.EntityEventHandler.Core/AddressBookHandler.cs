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

public class AddressBookHandler : IDistributedEventHandler<AddressBookEto>,
    IDistributedEventHandler<AddressBookDeleteEto>, ITransientDependency
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
            _logger.LogInformation("address book add success, userId:{userId}, address:{address}, name:{name}",
                eventData.UserId.ToString(), eventData.AddressInfo.Address, eventData.Name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "address book add error, {data}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(AddressBookDeleteEto eventData)
    {
        try
        {
            await _addressBookRepository.DeleteAsync(eventData.Id);
            _logger.LogInformation("address book delete success, id:{userId}", eventData.Id.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "address book delete error, {data}", JsonConvert.SerializeObject(eventData));
        }
    }
}