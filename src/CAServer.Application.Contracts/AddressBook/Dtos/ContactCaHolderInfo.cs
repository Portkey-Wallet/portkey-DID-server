using System;
using Orleans;

namespace CAServer.AddressBook.Dtos;

[GenerateSerializer]
public class ContactCaHolderInfo
{
    [Id(0)]
    public Guid UserId { get; set; }
    [Id(1)]
    public string CaHash { get; set; }
    [Id(2)]
    public string WalletName { get; set; }
    [Id(3)]
    public string Avatar { get; set; }
}