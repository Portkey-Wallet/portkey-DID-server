using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class ZeroHoldingsConfigIndex : CAServerEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    public string Status { get; set; }
}