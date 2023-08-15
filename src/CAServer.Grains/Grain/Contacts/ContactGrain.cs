using CAServer.Contacts;
using Orleans;
using CAServer.Grains.State.Contacts;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Contacts;

public class ContactGrain : Grain<ContactState>, IContactGrain
{
    private readonly IObjectMapper _objectMapper;

    public ContactGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<ContactGrainDto>> AddContactAsync(Guid userId, ContactGrainDto contactDto)
    {
        var result = new GrainResultDto<ContactGrainDto>();
        var contactNameGrain = GetContactNameGrain(userId, contactDto.Name);
        var addContactNameResult = await contactNameGrain.AddContactNameAsync(userId, contactDto.Name);
        if (!addContactNameResult)
        {
            result.Message = ContactMessage.ExistedMessage;
            return result;
        }

        State.Id = this.GetPrimaryKey();
        State.Index = GetIndex(contactDto.Name);
        State.Name = contactDto.Name;
        //State.UserId = userId;
        State.AddedUserId = userId;
        State.IsDeleted = false;
        State.ModificationTime = DateTime.UtcNow;
        State.Addresses = _objectMapper.Map<List<ContactAddressDto>, List<ContactAddress>>(contactDto.Addresses);

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<ContactGrainDto>> UpdateContactAsync(Guid userId, ContactGrainDto contactDto)
    {
        var result = new GrainResultDto<ContactGrainDto>();
        if (string.IsNullOrWhiteSpace(State.Name) || State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return result;
        }

        if (contactDto.Name != State.Name)
        {
            var contactNameGrain = GetContactNameGrain(userId, contactDto.Name);
            var addContactNameResult = await contactNameGrain.AddContactNameAsync(userId, contactDto.Name);
            if (!addContactNameResult)
            {
                result.Message = ContactMessage.NotExistMessage;
                return result;
            }

            var oldContactNameGrain = GetContactNameGrain(userId, State.Name);
            await oldContactNameGrain.DeleteContactNameAsync(userId, State.Name);
        }

        State.Index = GetIndex(contactDto.Name);
        State.Name = contactDto.Name;
        State.ModificationTime = DateTime.UtcNow;
        State.Addresses = _objectMapper.Map<List<ContactAddressDto>, List<ContactAddress>>(contactDto.Addresses);

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<ContactGrainDto>> DeleteContactAsync(Guid userId)
    {
        var result = new GrainResultDto<ContactGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return result;
        }

        var contactNameGrain = GetContactNameGrain(userId, State.Name);
        await contactNameGrain.DeleteContactNameAsync(userId, State.Name);

        State.IsDeleted = true;
        State.ModificationTime = DateTime.UtcNow;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<ContactGrainDto>> GetContactAsync(Guid userId)
    {
        var result = new GrainResultDto<ContactGrainDto>();
        
        if (State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
        
        return Task.FromResult(result);
    }

    private IContactNameGrain GetContactNameGrain(Guid userId, string name)
    {
        return GrainFactory.GetGrain<IContactNameGrain>(GrainIdHelper.GenerateGrainId(userId.ToString("N"), name));
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
}