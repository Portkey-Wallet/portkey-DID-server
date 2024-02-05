using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using Nest;

namespace CAServer.Entities.Es;

public class AccelerateRecoverIndex : CAServerEsEntity<string>, IIndexBuild
{
    public DateTime? CreateTime { get; set; }
    [Keyword] public Guid SessionId { get; set; }
    [Keyword] public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string IdentifierHash { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    [Keyword] public string RecoveryMessage { get; set; }
    [Keyword] public string RecoveryStatus { get; set; }
}