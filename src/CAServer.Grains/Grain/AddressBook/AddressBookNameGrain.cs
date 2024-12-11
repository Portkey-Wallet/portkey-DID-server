using CAServer.Grains.State.AddressBook;
using Orleans;

namespace CAServer.Grains.Grain.AddressBook;

public class AddressBookNameGrain : Grain<AddressBookNameState>, IAddressBookNameGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<bool> AddContactNameAsync(Guid userId, string name)
    {
        if (!await IsNameAvailableAsync())
        {
            return false;
        }

        State.UserId = userId;
        State.ContactName = name;
        State.IsDeleted = false;
        await WriteStateAsync();
        return true;
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

    private async Task<bool> IsNameAvailableAsync()
    {
        return string.IsNullOrWhiteSpace(State.ContactName) || State.IsDeleted;
    }
}