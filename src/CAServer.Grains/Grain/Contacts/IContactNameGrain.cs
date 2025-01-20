namespace CAServer.Grains.Grain.Contacts;

public interface IContactNameGrain : IGrainWithStringKey
{
    Task<bool> AddContactNameAsync(Guid userId, string name);
    Task DeleteContactNameAsync(Guid userId, string name);
    Task<bool> IsNameAvailableAsync(Guid userId, string name);
    Task<bool> IsNameExist(string name);
}