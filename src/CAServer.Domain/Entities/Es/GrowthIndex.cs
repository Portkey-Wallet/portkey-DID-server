using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class GrowthIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string InviteCode { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    [Keyword] public string ProjectCode { get; set; }
    [Keyword] public string ShortLinkCode { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsDeleted { get; set; }
}