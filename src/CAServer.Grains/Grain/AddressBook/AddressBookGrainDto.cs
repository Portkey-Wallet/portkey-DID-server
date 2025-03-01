using CAServer.AddressBook.Dtos;

namespace CAServer.Grains.Grain.AddressBook;

[GenerateSerializer]
public class AddressBookGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Index { get; set; }
    [Id(2)]
    public string Name { get; set; }
    [Id(3)]
    public ContactAddressInfo AddressInfo { get; set; }
    [Id(4)]
    public ContactCaHolderInfo CaHolderInfo { get; set; }
    [Id(5)]
    public Guid UserId { get; set; }
    [Id(6)]
    public bool IsDeleted { get; set; }
    [Id(7)]
    public DateTime CreateTime { get; set; }
    [Id(8)]
    public DateTime ModificationTime { get; set; }
}