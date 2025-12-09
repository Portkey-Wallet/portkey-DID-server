using Orleans;

namespace CAServer.AddressBook.Dtos;

[GenerateSerializer]
public class ContactAddressInfo
{
    [Id(0)]
    public string Network { get; set; }
    [Id(1)]
    public string NetworkName { get; set; }
    [Id(2)]
    public string ChainId { get; set; }
    [Id(3)]
    public string Address { get; set; }
    [Id(4)]
    public bool IsExchange { get; set; }
}