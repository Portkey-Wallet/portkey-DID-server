using System;

namespace CAServer.AddressBook.Migrate.Dto;

public class AddressBookMigrateDto
{
    public string Name { get; set; }
    public Guid UserId { get; set; }
    public string Address { get; set; }
    public string Network { get; set; }
    public string ChainId { get; set; }
    public bool IsExchange { get; set; }
}