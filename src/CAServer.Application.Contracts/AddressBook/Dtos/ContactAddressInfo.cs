namespace CAServer.AddressBook.Dtos;

public class ContactAddressInfo
{
    public string Network { get; set; }
    public string NetworkName { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public bool IsExchange { get; set; }
}