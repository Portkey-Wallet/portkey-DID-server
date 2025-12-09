using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class FreeMintIndex: CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    public string ImageUrl { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string TokenId { get; set; }
    public string Description { get; set; }
    [Keyword] public string Status { get; set; }
    [Keyword] public string Symbol { get; set; }
    public CollectionInfo CollectionInfo { get; set; }
    public List<MintTransactionInfo> TransactionInfos { get; set; } = new();
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class CollectionInfo
{
    [Keyword] public string CollectionName { get; set; }
    [Keyword] public string ImageUrl { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Symbol { get; set; }
}

public class MintTransactionInfo
{
    [Keyword] public string TransactionId { get; set; }
    public long BlockTime { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    [Keyword] public string TransactionResult { get; set; }
    public string ErrorMessage { get; set; }
}