using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class TransactionInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string CaAddress { get; set; }
    public long Time { get; set; }
}