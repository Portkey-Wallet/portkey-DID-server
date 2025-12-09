using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using Nest;

namespace CAServer.Entities.Es;

public class AccelerateRegisterIndex : CAServerEsEntity<string>, IIndexBuild
{
    public DateTime? CreateTime { get; set; }
    [Keyword] public Guid SessionId { get; set; }
    [Keyword] public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string IdentifierHash { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    [Keyword] public string RegisterMessage { get; set; }
    [Keyword] public string RegisterStatus { get; set; }
}