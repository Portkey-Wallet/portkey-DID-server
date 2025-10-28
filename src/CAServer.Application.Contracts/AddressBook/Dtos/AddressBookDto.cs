using System;

namespace CAServer.AddressBook.Dtos;

public class AddressBookDto
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public ContactAddressInfoDto AddressInfo { get; set; }
    public ContactCaHolderInfo CaHolderInfo { get; set; }
    public Guid UserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ModificationTime { get; set; }
}