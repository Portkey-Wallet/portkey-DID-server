using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class FreeMintNftSyncIndex: CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string TransactionId { get; set; }
    public long BlockNumber { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    [Keyword] public string TransactionResult { get; set; }
    public string ErrorMessage { get; set; }
}