using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class UpgradeInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsPopup { get; set; }
    [Keyword] public string Version { get; set; }
}