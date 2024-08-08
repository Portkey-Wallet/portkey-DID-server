using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class InviteRepairIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ProjectCode { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    public DateTime RegisterTime { get; set; }
    public DateTime UpdateTime { get; set; }
}