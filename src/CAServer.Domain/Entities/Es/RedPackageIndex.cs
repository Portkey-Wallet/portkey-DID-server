using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class RedPackageIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid RedPackageId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MinAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    [Keyword] public Guid SenderId { get; set; }
    [Keyword] public long CreateTime { get; set; }
    [Keyword] public long EndTime { get; set; }
    [Keyword] public long ExpireTime { get; set; }
    [Keyword] public string Symbol { get; set; }
    public int Decimal { get; set; }
    public int Count { get; set; }
    [Keyword] public string ChannelUuid { get; set; }
    public string SendUuid { get; set; }
    public string Message { get; set; }
    [Keyword] public RedPackageType Type { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string TransactionResult { get; set; }
    public string ErrorMessage { get; set; }
    public string SenderRelationToken { get; set; }
    public RedPackageTransactionStatus TransactionStatus { get; set; }
}