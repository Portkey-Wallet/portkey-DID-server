using System;
using System.Collections.Generic;

namespace CAServer.Contacts;

public class ImInfoDto
{
    public string RelationId { get; set; }
    public Guid PortkeyId { get; set; }
    public string Name { get; set; }
    public List<AddressWithChain> AddressWithChain { get; set; } = new();
}

public class AddressWithChain
{
    public string Address { get; set; }
    public string ChainName { get; set; }
}