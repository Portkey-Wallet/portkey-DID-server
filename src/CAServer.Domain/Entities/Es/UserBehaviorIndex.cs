using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class UserBehaviorIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public string DappName { get; set; }
    [Keyword] public string Device { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string UserId { get; set; }
    [Keyword] public bool Result { get; set; }
    [Keyword] public string Action { get; set; }
    [Keyword] public string Referer { get; set; }
    [Keyword] public string UserAgent { get; set; }
    public string Origin { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string SessionId { get; set; }
    [Keyword] public long Timestamp { get; set; }
}