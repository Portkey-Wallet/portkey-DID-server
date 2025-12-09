using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class AccountReportIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ClientType { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string ProjectCode { get; set; }
    [Keyword] public string OperationType { get; set; }
    public DateTime CreateTime { get; set; }
}