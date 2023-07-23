using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class TestIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Name { get; set; }
    public int Age { get; set; }
    [Keyword] public string Address { get; set; }
}