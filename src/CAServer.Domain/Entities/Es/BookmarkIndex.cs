using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class BookmarkIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Url { get; set; }
    [Keyword] public long ModificationTime { get; set; }
    public int Index { get; set; }
}