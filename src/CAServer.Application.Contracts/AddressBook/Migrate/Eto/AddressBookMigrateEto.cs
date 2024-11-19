using System;

namespace CAServer.AddressBook.Migrate.Eto;

public class AddressBookMigrateEto
{
    public Guid OriginalContactId { get; set; }
    public Guid NewContactId { get; set; }
    public Guid UserId { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string FailType { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}