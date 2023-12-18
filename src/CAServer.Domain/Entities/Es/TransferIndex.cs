using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class TransferIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public long Amount { get; set; }
    [Keyword] public Guid SenderId { get; set; }
    [Keyword] public string Type { get; set; }
    [Keyword] public Guid ToUserId { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string Memo { get; set; }
    [Keyword] public string ChannelUuid { get; set; }
    [Keyword] public string RawTransaction { get; set; }
    public string Message { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string BlockHash { get; set; }
    [Keyword] public string TransactionResult { get; set; }
    [Keyword] public string TransactionStatus { get; set; }
    public string ErrorMessage { get; set; }
    public string SenderRelationToken { get; set; }
    public string SenderPortkeyToken { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset ModificationTime { get; set; }
}