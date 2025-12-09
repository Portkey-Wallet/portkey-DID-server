using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class TwitterStatisticIndex: CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public long UpdateTime { get; set; }
    public int CallCount { get; set; }
}