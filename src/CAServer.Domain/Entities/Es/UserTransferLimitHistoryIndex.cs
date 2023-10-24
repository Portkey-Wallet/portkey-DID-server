using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class UserTransferLimitHistoryIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    public override Guid Id { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string ChainId { get; set; }
}