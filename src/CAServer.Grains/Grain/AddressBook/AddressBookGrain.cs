using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.State.AddressBook;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.AddressBook;

public class AddressBookGrain : Grain<AddressBookState>, IAddressBookGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AddressBookGrain> _logger;

    public AddressBookGrain(IObjectMapper objectMapper, ILogger<AddressBookGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task<GrainResultDto<AddressBookGrainDto>> AddContactAsync(AddressBookGrainDto addressBookDto)
    {
        var result = new GrainResultDto<AddressBookGrainDto>();
        if (!addressBookDto.Name.IsNullOrWhiteSpace())
        {
            var contactNameGrain = GetContactNameGrain(addressBookDto.UserId, addressBookDto.Name);
            var addContactNameResult = await contactNameGrain.AddContactNameAsync(addressBookDto.UserId, addressBookDto.Name);
            if (!addContactNameResult)
            {
                result.Message = AddressBookMessage.ExistedMessage;
                return result;
            }
        }

        State = _objectMapper.Map<AddressBookGrainDto, AddressBookState>(addressBookDto);
        State.Id = this.GetPrimaryKey();
        State.IsDeleted = false;
        State.CreateTime = DateTime.UtcNow;
        State.ModificationTime = DateTime.UtcNow;
        SetIndex();
        
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<AddressBookState, AddressBookGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<AddressBookGrainDto>> UpdateContactAsync(AddressBookGrainDto contactDto)
    {
        var result = new GrainResultDto<AddressBookGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = AddressBookMessage.NotExistMessage;
            return result;
        }

        if (State.Name != contactDto.Name)
        {
            var contactNameGrain = GetContactNameGrain(contactDto.UserId, contactDto.Name);
            var addContactNameResult = await contactNameGrain.AddContactNameAsync(contactDto.UserId, contactDto.Name);
            if (!addContactNameResult)
            {
                result.Message = ContactMessage.ExistedMessage;
                return result;
            }

            var oldContactNameGrain = GetContactNameGrain(contactDto.UserId, State.Name);
            await oldContactNameGrain.DeleteContactNameAsync(contactDto.UserId, State.Name);
        }
        
        State = _objectMapper.Map<AddressBookGrainDto, AddressBookState>(contactDto);
        State.Id = this.GetPrimaryKey();
        State.IsDeleted = false;
        State.ModificationTime = DateTime.UtcNow;
        SetIndex();
        
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<AddressBookState, AddressBookGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<AddressBookGrainDto>> DeleteContactAsync(Guid userId)
    {
        var result = new GrainResultDto<AddressBookGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = AddressBookMessage.NotExistMessage;
            return result;
        }

        if (!State.Name.IsNullOrWhiteSpace())
        {
            var contactNameGrain = GetContactNameGrain(userId, State.Name);
            await contactNameGrain.DeleteContactNameAsync(userId, State.Name);
        }

        State.IsDeleted = true;
        State.ModificationTime = DateTime.UtcNow;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<AddressBookState, AddressBookGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<AddressBookGrainDto>> GetContactAsync()
    {
        var result = new GrainResultDto<AddressBookGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<AddressBookState, AddressBookGrainDto>(State);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<AddressBookGrainDto>> UpdateContactInfo(string walletName, string avatar)
    {
        var result = new GrainResultDto<AddressBookGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return result;
        }
        
        if (State.CaHolderInfo == null)
        {
            result.Message = ContactMessage.HolderNullMessage;
            return result;
        }

        State.CaHolderInfo.WalletName = walletName;
        if (!avatar.IsNullOrWhiteSpace())
        {
            State.CaHolderInfo.Avatar = avatar;
        }
        
        SetIndex();
        State.ModificationTime = DateTime.UtcNow;
        await WriteStateAsync();
        
        result.Success = true;
        result.Data = _objectMapper.Map<AddressBookState, AddressBookGrainDto>(State);
        return result;
    }
    
    private IAddressBookNameGrain GetContactNameGrain(Guid userId, string name)
    {
        return GrainFactory.GetGrain<IAddressBookNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
    }

    private string GetIndex(string name)
    {
        var firstChar = char.ToUpperInvariant(name[0]);
        if (firstChar >= 'A' && firstChar <= 'Z')
        {
            return firstChar.ToString();
        }

        return "#";
    }

    private void SetIndex()
    {
        if (!State.Name.IsNullOrWhiteSpace())
        {
            State.Index = GetIndex(State.Name);
        }
        else if (State.CaHolderInfo != null && !State.CaHolderInfo.WalletName.IsNullOrWhiteSpace())
        {
            State.Index = GetIndex(State.CaHolderInfo.WalletName);
        }
        else
        {
            State.Index = string.Empty;
        }
    }
}