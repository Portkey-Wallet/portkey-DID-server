using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class CaHolderTransactionIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string MethodName { get; set; }
    public long Timestamp { get; set; }
    [Keyword] public string Status { get; set; }
    [Keyword] public string ToContractAddress { get; set; }
    public bool IsManagerConsumer { get; set; }
    [Keyword] public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    [Keyword] public string PreviousBlockHash { get; set; }
}