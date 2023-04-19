using CAServer.Grains.State.Contacts;
using Orleans;

namespace CAServer.Grains.Grain.Contacts;

public class ContactNameGrain : Grain<ContactNameState>, IContactNameGrain
{
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

    public async Task<bool> AddContactNameAsync(Guid userId, string name)
    {
        if (await IsNameAvailableAsync(userId, name))
        {
            State.UserId = userId;
            State.ContactName = name;
            State.IsDeleted = false;
            await WriteStateAsync();
            return true;
        }

        return false;
    }

    public async Task DeleteContactNameAsync(Guid userId, string name)
    {
        if (State.UserId == userId && State.ContactName == name)
        {
            State.IsDeleted = true;
            await WriteStateAsync();
        }
    }

    public async Task<bool> IsNameExist(string name)
    {
        return State.ContactName == name && !State.IsDeleted;
    }

    public async Task<bool> IsNameAvailableAsync(Guid userId, string name)
    {
        return string.IsNullOrWhiteSpace(State.ContactName) || State.IsDeleted;
    }
}