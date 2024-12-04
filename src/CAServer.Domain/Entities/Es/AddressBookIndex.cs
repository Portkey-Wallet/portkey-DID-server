using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class AddressBookIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string Index { get; set; }
    [Wildcard] public string Name { get; set; }
    public AddressInfo AddressInfo { get; set; }
    public ContactCaHolderInfo CaHolderInfo { get; set; }
    [Keyword] public Guid UserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ModificationTime { get; set; }
}

public class AddressInfo
{
    [Keyword] public string Network { get; set; }
    [Keyword] public string NetworkName { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public bool IsExchange { get; set; }
}

public class ContactCaHolderInfo
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Wildcard] public string WalletName { get; set; }
    public string Avatar { get; set; }
}