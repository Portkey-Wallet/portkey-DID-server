using CAServer.AddressBook.Dtos;

namespace CAServer.Grains.Grain.AddressBook;

public class AddressBookGrainDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public ContactAddressInfo AddressInfo { get; set; }
    public ContactCaHolderInfo CaHolderInfo { get; set; }
    public Guid UserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ModificationTime { get; set; }
}