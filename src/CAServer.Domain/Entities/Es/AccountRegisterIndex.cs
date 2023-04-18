using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using Nest;

namespace CAServer.Entities.Es;

public class AccountRegisterIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    public DateTime? CreateTime { get; set; }
    [Keyword] public string ChainId { get; set; }
    public Manager Manager { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    public GuardianAccountInfo GuardianAccountInfo { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    [Keyword] public string RegisterMessage { get; set; }
    [Keyword] public string RegisterStatus { get; set; }
}