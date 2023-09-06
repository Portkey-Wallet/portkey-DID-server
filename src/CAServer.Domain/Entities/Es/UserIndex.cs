using System;
using System.Collections.Generic;
using Nest;

namespace CAServer.Entities.Es;

public class UserIndex : CAServerEsEntity<Guid>
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string CaHash { get; set; }
    public List<CaAddressInfo> CaAddresses { get; set; }
    [Keyword] public string RelationId { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Avatar { get; set; }
    [Keyword] public long CreateTime { get; set; }
}

public class CaAddressInfo
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ChainName { get; set; }
    [Keyword] public string Address { get; set; }
}