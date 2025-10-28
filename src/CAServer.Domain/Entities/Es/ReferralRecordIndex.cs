using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class ReferralRecordIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string ReferralCode { get; set; }
    [Keyword] public string ReferralCaHash { get; set; }
    [Keyword] public string ReferralAddress { get; set; }
    public int IsDirectlyInvite { get; set; } = 0;
    public DateTime ReferralDate { get; set; }

    public int ReferralType { get; set; } = 0;
}