namespace CAServer.Grains.Grain.AddressBook;

public interface IAddressBookNameGrain : IGrainWithStringKey
{
    Task<bool> AddContactNameAsync(Guid userId, string name);
    Task DeleteContactNameAsync(Guid userId, string name);
    Task<bool> IsNameExist(string name);
}