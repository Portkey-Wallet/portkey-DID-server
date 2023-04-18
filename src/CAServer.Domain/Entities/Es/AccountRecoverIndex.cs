using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using Nest;

namespace CAServer.Entities.Es;

public class AccountRecoverIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    public DateTime? CreateTime { get; set; }
    [Keyword] public string ChainId { get; set; }
    public Manager Manager { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }

    public List<GuardianAccountInfo> GuardianApproved { get; set; }
    [Keyword] public string LoginGuardianAccount { get; set; }
    public DateTime? RecoveryTime { get; set; }
    public bool? RecoverySuccess { get; set; }
    [Keyword] public string RecoveryMessage { get; set; }
    [Keyword] public string RecoveryStatus { get; set; }
}