using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AddressBook.Dtos;
using CAServer.AddressBook.Etos;
using CAServer.AddressBook.Migrate.Dto;
using CAServer.AddressBook.Migrate.Eto;
using CAServer.AddressBook.Provider;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.AddressBook;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.AddressBook.Migrate;

[RemoteService(false), DisableAuditing]
public class AddressBookMigrateService : IAddressBookMigrateService, ISingletonDependency
{
    private readonly ILogger<AddressBookMigrateService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IAddressBookProvider _addressBookProvider;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private readonly IOptionsMonitor<ContactMigrateOptions> _options;

    public AddressBookMigrateService(ILogger<AddressBookMigrateService> logger, IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IAddressBookProvider addressBookProvider, INESTRepository<ContactIndex, Guid> contactRepository,
        IOptionsMonitor<ContactMigrateOptions> options)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _addressBookProvider = addressBookProvider;
        _contactRepository = contactRepository;
        _options = options;
    }

    public async Task MigrateAsync()
    {
        // get index
        _logger.LogInformation("[AddressBookMigrate] migrate service start.");
        var contacts = await GetContactsAsync(Guid.Empty, 0, _options.CurrentValue.MigrateCount);
        if (contacts.IsNullOrEmpty())
        {
            _logger.LogInformation("[AddressBookMigrate] contacts empty.");
            return;
        }

        contacts = contacts.Where(t => t.IsDeleted == false).ToList();
        _logger.LogInformation("[AddressBookMigrate] need handle contact count:{0}.", contacts.Count);
        foreach (var contact in contacts)
        {
            try
            {
                var migrateDto = CreateMigrateDto(contact);
                await CreateContactAsync(contact, migrateDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[AddressBookMigrate] ContactType Error, contactId:{0}, message:{1}, stack:{2}",
                    contact.Id, e.Message, e.StackTrace ?? "-");

                await PublishAsync(contact.Id, contact.UserId, e.Message);
            }
        }

        _logger.LogInformation("[AddressBookMigrate] migrate service end.");
    }

    private async Task CreateContactAsync(ContactIndex contact, List<AddressBookMigrateDto> migrateDtoList)
    {
        foreach (var dto in migrateDtoList)
        {
            try
            {
                var createResult = await CreateAsync(dto);
                _logger.LogInformation("[AddressBookMigrate] success, contactId:{0}", createResult.Id);
                await PublishAsync(createResult, contact.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "[AddressBookMigrate] ContactItem Error, contactId:{0},chainId:{1}, address:{2} message:{3}, stack:{4}",
                    contact.Id, dto.ChainId, dto.Address,
                    e.Message,
                    e.StackTrace ?? "-");

                await PublishAsync(contact.Id, e.Message, dto);
                continue;
            }
        }
    }

    private async Task PublishAsync(AddressBookDto addressBookDto, Guid originalId)
    {
        var eto = new AddressBookMigrateEto()
        {
            OriginalContactId = originalId,
            NewContactId = addressBookDto.Id,
            ChainId = addressBookDto.AddressInfo.ChainId,
            Address = addressBookDto.AddressInfo.Address,
            Status = "success",
            CreateTime = addressBookDto.CreateTime,
            UpdateTime = addressBookDto.ModificationTime
        };

        await _distributedEventBus.PublishAsync(eto, false, false);
    }

    private async Task PublishAsync(Guid originalId, Guid userId, string message)
    {
        var eto = new AddressBookMigrateEto()
        {
            OriginalContactId = originalId,
            UserId = userId,
            FailType = "contact",
            Status = "fail",
            Message = message,
            CreateTime = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow
        };

        await _distributedEventBus.PublishAsync(eto, false, false);
    }

    private async Task PublishAsync(Guid originalId, string message, AddressBookMigrateDto migrateDto)
    {
        var eto = new AddressBookMigrateEto()
        {
            OriginalContactId = originalId,
            ChainId = migrateDto.ChainId,
            Address = migrateDto.Address,
            UserId = migrateDto.UserId,
            FailType = "item",
            Status = "fail",
            Message = message,
            CreateTime = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow
        };

        await _distributedEventBus.PublishAsync(eto, false, false);
    }

    public async Task<AddressBookDto> CreateAsync(AddressBookMigrateDto contactDto)
    {
        contactDto.Address = GetAddress(contactDto.Address);
        await CheckAddressAsync(contactDto.UserId, contactDto.Network, contactDto.ChainId, contactDto.Address,
            contactDto.IsExchange);

        var addressBookDto = await GetAddressBookDtoAsync(contactDto);
        addressBookDto.UserId = contactDto.UserId;
        var addressBookGrain = _clusterClient.GetGrain<IAddressBookGrain>(Guid.NewGuid());
        var result =
            await addressBookGrain.AddContactAsync(
                _objectMapper.Map<AddressBookDto, AddressBookGrainDto>(addressBookDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message,
                code: result.Message == AddressBookMessage.ExistedMessage ? AddressBookMessage.NameExistedCode : null);
        }

        var eto = _objectMapper.Map<AddressBookGrainDto, AddressBookEto>(result.Data);
        await _distributedEventBus.PublishAsync(eto);

        var resultDto = _objectMapper.Map<AddressBookGrainDto, AddressBookDto>(result.Data);
        return resultDto;
    }

    private async Task CheckAddressAsync(Guid userId, string network, string chainId, string address, bool isExchange)
    {
        if (!ValidateAddress(address))
        {
            throw new UserFriendlyException("Invalid address.", AddressBookMessage.AddressInvalidCode);
        }

        await CheckSelfAsync(userId, address);

        // check if address already exist
        var contact =
            await _addressBookProvider.GetContactByAddressInfoAsync(userId, network, chainId, address, isExchange);

        if (contact != null)
        {
            _logger.LogInformation("### contact:{0}", JsonConvert.SerializeObject(contact));
            throw new UserFriendlyException("This address has already been taken in other contacts");
        }
    }

    private async Task CheckSelfAsync(Guid userId, string address)
    {
        // check self
        var holder = await _addressBookProvider.GetCaHolderAsync(userId, string.Empty);
        if (holder == null)
        {
            throw new UserFriendlyException("Holder not found");
        }

        var guardianDto = await _addressBookProvider.GetGuardianInfoAsync(new List<string>() { }, holder.CaHash);
        if (guardianDto.CaHolderInfo.Select(t => t.CaAddress).ToList().Contains(address))
        {
            throw new UserFriendlyException("Unable to add yourself to your Contacts");
        }
    }

    private bool ValidateAddress(string address)
    {
        try
        {
            return AElf.AddressHelper.VerifyFormattedAddress(AddressHelper.ToShortAddress(address));
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private string GetAddress(string address)
    {
        return AddressHelper.ToShortAddress(address);
    }

    private async Task<AddressBookDto> GetAddressBookDtoAsync(AddressBookMigrateDto input)
    {
        var caHolderInfo = await GetHolderInfoAsync(input.Address);
        var addressBookDto = new AddressBookDto
        {
            Name = input.Name,
            AddressInfo = new ContactAddressInfoDto
            {
                Address = input.Address,
                ChainId = input.ChainId,
                Network = input.Network,
                NetworkName = input.ChainId,
                IsExchange = input.IsExchange
            },
            CaHolderInfo = caHolderInfo
        };

        return addressBookDto;
    }

    private async Task<Dtos.ContactCaHolderInfo> GetHolderInfoAsync(string address)
    {
        var guardiansDto =
            await _addressBookProvider.GetGuardianInfoAsync(new List<string> { address }, string.Empty);
        var caHash = guardiansDto?.CaHolderInfo?.FirstOrDefault()?.CaHash;
        if (caHash.IsNullOrWhiteSpace()) return null;

        var caHolder = await _addressBookProvider.GetCaHolderAsync(Guid.Empty, caHash);
        return _objectMapper.Map<CAHolderIndex, Dtos.ContactCaHolderInfo>(caHolder);
    }

    public async Task<List<ContactIndex>> GetContactsAsync(Guid userId, int skip, int limit)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContactIndex>, QueryContainer>>() { };
        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<ContactIndex> f) => f.Bool(b => b.Must(mustQuery));
        var contact = await _contactRepository.GetListAsync(Filter, sortExp: k => k.CreateTime,
            sortType: SortOrder.Descending, skip: skip, limit: limit);
        if (contact.Item1 <= 0)
        {
            return new List<ContactIndex>();
        }

        return contact.Item2;
    }

    private List<AddressBookMigrateDto> CreateMigrateDto(ContactIndex contact)
    {
        var result = new List<AddressBookMigrateDto>();
        var name = !contact.Name.IsNullOrEmpty() ? contact.Name : contact.CaHolderInfo?.WalletName;
        if (name.IsNullOrEmpty())
        {
            _logger.LogError("Contact name is null, contact:{0}", JsonConvert.SerializeObject(contact));
            throw new Exception("Contact name is null");
        }

        if (contact.Addresses.IsNullOrEmpty())
        {
            _logger.LogError("Contact address list is empty, contact:{0}", JsonConvert.SerializeObject(contact));
            throw new Exception("Contact address list is empty");
        }

        foreach (var address in contact.Addresses)
        {
            var migrateDto = new AddressBookMigrateDto
            {
                UserId = contact.UserId,
                Name = name,
                Address = address.Address,
                ChainId = address.ChainId,
                Network = "aelf",
                IsExchange = false
            };

            result.Add(migrateDto);
        }

        return result;
    }
}