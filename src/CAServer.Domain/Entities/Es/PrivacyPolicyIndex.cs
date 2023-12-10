using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class PrivacyPolicyIndex : CAServerEsEntity<string>, IIndexBuild
{
    public int PolicyVersion { get; set; }
    [Keyword]
    public string CaHash { get; set; }
    [Keyword]
    public string Origin { get; set; }
    public int Scene { get; set; }
    [Keyword]
    public string ManagerAddress { get; set; }
    [Keyword]
    public string PolicyId { get; set; }
    public long Timestamp { get; set; }
    public string Content { get; set; }
}