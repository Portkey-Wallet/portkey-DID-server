using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.AddressBook.Dtos;
using CAServer.AddressBook.Etos;
using CAServer.AddressBook.Provider;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.AddressBook;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.AddressBook;

[RemoteService(false), DisableAuditing]
public class AddressBookAppService : CAServerAppService, IAddressBookAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IAddressBookProvider _addressBookProvider;
    private readonly VariablesOptions _variablesOptions;

    public AddressBookAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IAddressBookProvider addressBookProvider, IOptionsSnapshot<VariablesOptions> variablesOptions)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _addressBookProvider = addressBookProvider;
        _variablesOptions = variablesOptions.Value;
    }

    public async Task<AddressBookDto> CreateAsync(AddressBookCreateRequestDto requestDto)
    {
        var userId = CurrentUser.GetId();
        // var existed = await CheckNameExistAsync(userId, requestDto.Name);
        // if (existed)
        // {
        //     throw new UserFriendlyException(ContactMessage.ExistedMessage);
        // }

        // todo: check address valid
        await CheckAddressAsync(userId, requestDto.Network, requestDto.ChainId, requestDto.Address);

        var addressBookDto = await GetAddressBookDtoAsync(requestDto);
        addressBookDto.UserId = userId;
        var addressBookGrain = _clusterClient.GetGrain<IAddressBookGrain>(GuidGenerator.Create());
        var result =
            await addressBookGrain.AddContactAsync(
                ObjectMapper.Map<AddressBookDto, AddressBookGrainDto>(addressBookDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var eto = ObjectMapper.Map<AddressBookGrainDto, AddressBookEto>(result.Data);
        await _distributedEventBus.PublishAsync(eto);

        var resultDto = ObjectMapper.Map<AddressBookGrainDto, AddressBookDto>(result.Data);
        SetNetworkImage(resultDto);
        return resultDto;
    }

    public async Task<AddressBookDto> UpdateAsync(AddressBookUpdateRequestDto requestDto)
    {
        var userId = CurrentUser.GetId();
        var addressBookGrain = _clusterClient.GetGrain<IAddressBookGrain>(requestDto.Id);
        var contactResult = await addressBookGrain.GetContactAsync();
        if (!contactResult.Success)
        {
            throw new UserFriendlyException(contactResult.Message);
        }

        await CheckAddressAsync(userId, requestDto.Network, requestDto.ChainId, requestDto.Address);

        var addressBookDto = await GetAddressBookDtoAsync(requestDto);
        addressBookDto.UserId = userId;
        var result =
            await addressBookGrain.UpdateContactAsync(
                ObjectMapper.Map<AddressBookDto, AddressBookGrainDto>(addressBookDto));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        var eto = ObjectMapper.Map<AddressBookGrainDto, AddressBookEto>(result.Data);
        await _distributedEventBus.PublishAsync(eto);

        var resultDto = ObjectMapper.Map<AddressBookGrainDto, AddressBookDto>(result.Data);
        SetNetworkImage(resultDto);
        return resultDto;
    }

    public async Task DeleteAsync(AddressBookDeleteRequestDto requestDto)
    {
        var addressBookGrain = _clusterClient.GetGrain<IAddressBookGrain>(requestDto.Id);
        var result = await addressBookGrain.DeleteContactAsync(CurrentUser.GetId());
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<AddressBookGrainDto, AddressBookDeleteEto>(result.Data));
    }

    public async Task<AddressBookExistDto> ExistAsync(string name)
    {
        var userId = CurrentUser.GetId();
        var addressBookNameGrain =
            _clusterClient.GetGrain<IAddressBookNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        var existed = await addressBookNameGrain.IsNameExist(name);

        return new AddressBookExistDto
        {
            Existed = existed
        };
    }

    public async Task<PagedResultDto<AddressBookDto>> GetListAsync(AddressBookListRequestDto requestDto)
    {
        var (totalCount, contacts) = await _addressBookProvider.GetListAsync(CurrentUser.GetId(), requestDto);
        var contactList = ObjectMapper.Map<List<AddressBookIndex>, List<AddressBookDto>>(contacts);

        foreach (var contact in contactList)
        {
            contact.AddressInfo.NetworkImage = _variablesOptions.ImageMap.GetOrDefault(contact.AddressInfo.Network);
        }

        return new PagedResultDto<AddressBookDto>
        {
            TotalCount = totalCount,
            Items = contactList
        };
    }

    public async Task<GetNetworkListDto> GetNetworkListAsync()
    {
        return new GetNetworkListDto()
        {
            NetworkList = new List<AddressBookNetwork>()
            {
                new AddressBookNetwork()
                {
                    ChainId = "AELF",
                    Name = "aelf MainChain",
                    Network = "aelf",
                    ImageUrl = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/mainChain.png"
                }
            }
        };
    }

    private async Task<bool> CheckNameExistAsync(Guid userId, string name)
    {
        if (name.IsNullOrWhiteSpace()) return false;

        var contactNameGrain =
            _clusterClient.GetGrain<IAddressBookNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
        return await contactNameGrain.IsNameExist(name);
    }

    private async Task CheckAddressAsync(Guid userId, string network, string chainId, string address)
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

        // check if address already exist
        var contact = await _addressBookProvider.GetContactByAddressInfoAsync(userId, network, chainId, address);
        if (contact != null)
        {
            throw new UserFriendlyException("This address has already been taken in other contacts");
        }
    }

    private async Task<AddressBookDto> GetAddressBookDtoAsync(AddressBookCreateRequestDto input, Guid? contactId = null)
    {
        var addressBookDto = new AddressBookDto
        {
            Name = input.Name,
            AddressInfo = new ContactAddressInfoDto
            {
                Address = input.Address,
                ChainId = input.ChainId,
                Network = input.Network,
                NetworkName = GetNetworkName(input.Network),
                IsExchange = input.IsExchange
            },
            CaHolderInfo = await GetHolderInfoAsync(input.Address)
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
        return ObjectMapper.Map<CAHolderIndex, Dtos.ContactCaHolderInfo>(caHolder);
    }

    private string GetNetworkName(string network)
    {
        throw new NotImplementedException();
    }

    private void SetNetworkImage(AddressBookDto addressBookDto)
    {
        addressBookDto.AddressInfo.NetworkImage =
            _variablesOptions.ImageMap.GetOrDefault(addressBookDto.AddressInfo.Network);
    }
}