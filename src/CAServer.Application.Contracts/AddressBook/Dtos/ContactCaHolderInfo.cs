using System;

namespace CAServer.AddressBook.Dtos;

public class ContactCaHolderInfo
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
    public string Avatar { get; set; }
}