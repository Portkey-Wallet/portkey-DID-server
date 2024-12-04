using System.Threading.Tasks;

namespace CAServer.AddressBook.Migrate;

public interface IAddressBookMigrateService
{
    Task MigrateAsync();
}