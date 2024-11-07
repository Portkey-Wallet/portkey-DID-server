using System.Collections.Generic;

namespace CAServer.AddressBook.Dtos;

public class GetNetworkListDto
{
    public List<AddressBookNetwork> NetworkList { get; set; }
}

public class AddressBookNetwork
{
    public string Network { get; set; }
    public string Name { get; set; }
    public string ChainId { get; set; }
    public string ImageUrl { get; set; }
}