using CAServer.Contacts;
using Orleans;
using CAServer.Grains.State.Contacts;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Contacts;

public class ContactGrain : Grain<ContactState>, IContactGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ContactGrain> _logger;

    public ContactGrain(IObjectMapper objectMapper, ILogger<ContactGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
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

        try
        {
            if (!contactDto.Name.IsNullOrWhiteSpace())
            {
                var contactNameGrain = GetContactNameGrain(userId, contactDto.Name);
                var addContactNameResult = await contactNameGrain.AddContactNameAsync(userId, contactDto.Name);
                if (!addContactNameResult)
                {
                    result.Message = ContactMessage.ExistedMessage;
                    return result;
                }
            }

            State.Id = this.GetPrimaryKey();
            State.Name = contactDto.Name;
            State.UserId = userId;
            State.IsDeleted = false;
            State.CreateTime = DateTime.UtcNow;
            State.ModificationTime = DateTime.UtcNow;
            State.Addresses = _objectMapper.Map<List<ContactAddressDto>, List<ContactAddress>>(contactDto.Addresses);

            SetIndex();
            await WriteStateAsync();

            result.Success = true;
            result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add contact error, userId:{userId}", userId.ToString());
            result.Message = e.Message;
            return result;
        }
    }

    public async Task<GrainResultDto<ContactGrainDto>> UpdateContactAsync(Guid userId, ContactGrainDto contactDto)
    {
        var result = new GrainResultDto<ContactGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = ContactMessage.NotExistMessage;
            return result;
        }

        if (State.Name.IsNullOrWhiteSpace() && contactDto.Name.IsNullOrWhiteSpace())
        {
        }
        else if (State.Name.IsNullOrWhiteSpace() && !contactDto.Name.IsNullOrWhiteSpace())
        {
            var contactNameGrain = GetContactNameGrain(userId, contactDto.Name);
            var addContactNameResult = await contactNameGrain.AddContactNameAsync(userId, contactDto.Name);
            if (!addContactNameResult)
            {
                result.Message = ContactMessage.ExistedMessage;
                return result;
            }
        }
        else if (!State.Name.IsNullOrWhiteSpace() && contactDto.Name.IsNullOrWhiteSpace())
        {
            var oldContactNameGrain = GetContactNameGrain(userId, State.Name);
            await oldContactNameGrain.DeleteContactNameAsync(userId, State.Name);
        }
        else if (contactDto.Name != State.Name)
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

        State.Name = contactDto.Name;
        State.ModificationTime = DateTime.UtcNow;
        State.Addresses = _objectMapper.Map<List<ContactAddressDto>, List<ContactAddress>>(contactDto.Addresses);

        SetIndex();
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

        if (!State.Name.IsNullOrWhiteSpace())
        {
            var contactNameGrain = GetContactNameGrain(userId, State.Name);
            await contactNameGrain.DeleteContactNameAsync(userId, State.Name);
        }
        
        State.IsDeleted = true;
        State.ModificationTime = DateTime.UtcNow;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ContactState, ContactGrainDto>(State);
        return result;
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
        else if (State.ImInfo != null && !State.ImInfo.Name.IsNullOrWhiteSpace())
        {
            State.Index = GetIndex(State.ImInfo.Name);
        }
        else
        {
            State.Index = string.Empty;
        }
    }
}