using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class RedDotIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public List<RedDotInfo> RedDotInfos { get; set; } = new();
}

public class RedDotInfo
{
    [Keyword] public string RedDotType { get; set; }
    [Keyword] public string Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ReadTime { get; set; }
}