using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class AddressInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
}