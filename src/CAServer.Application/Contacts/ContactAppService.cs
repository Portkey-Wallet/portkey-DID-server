using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace CAServer.Contacts;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContactAppService : CAServerAppService, IContactAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ContactOptions _contactOptions;

    public ContactAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IOptionsSnapshot<ContactOptions> contactOptions)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contactOptions = contactOptions.Value;
    }

    public async Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input)
    {
        CheckAddressCount(input);
        var userId = CurrentUser.GetId();

        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), input.Name));
        var existed = await contactNameGrain.IsNameExist(input.Name);
        if (existed)
        {
            throw new UserFriendlyException(ContactMessage.ExistedMessage);
        }

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(GuidGenerator.Create());
        var result =
            await contactGrain.AddContactAsync(userId,
                ObjectMapper.Map<CreateUpdateContactDto, ContactGrainDto>(input));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactCreateEto>(result.Data));

        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
    }

    public async Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input)
    {
        CheckAddressCount(input);
        var userId = CurrentUser.GetId();

        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);
        var result =
            await contactGrain.UpdateContactAsync(userId,
                ObjectMapper.Map<CreateUpdateContactDto, ContactGrainDto>(input));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));

        return ObjectMapper.Map<ContactGrainDto, ContactResultDto>(result.Data);
    }

    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var contactGrain = _clusterClient.GetGrain<IContactGrain>(id);

        var result = await contactGrain.DeleteContactAsync(userId);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<ContactGrainDto, ContactUpdateEto>(result.Data));
    }

    public async Task<ContractExistDto> GetExistAsync(string name)
    {
        var userId = CurrentUser.GetId();
        var contactNameGrain =
            _clusterClient.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        var existed = await contactNameGrain.IsNameExist(name);

        return new ContractExistDto
        {
            Existed = existed
        };
    }

    private void CheckAddressCount(CreateUpdateContactDto input)
    {
        if (input.Addresses.Count <= _contactOptions.MaxAddressesCount) return;

        var error = new ValidationResult("Too many addresses!", new[] { nameof(input.Addresses) });
        throw new AbpValidationException(new List<ValidationResult> { error });
    }
}