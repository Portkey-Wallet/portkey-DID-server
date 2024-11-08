using System.Collections.Generic;

namespace CAServer.AddressBook.Dtos;

public class GetSupportNetworkDto
{
    public Dictionary<string, Dictionary<string, List<NetworkBasicInfo>>> SupportedNetworks { get; set; }
}

public class NetworkBasicInfo
{
    public string Network { get; set; }
    public string Name { get; set; }
}