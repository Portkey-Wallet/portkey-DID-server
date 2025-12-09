using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class UserExtraInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string GuardianType { get; set; }
    [Keyword] public string FullName { get; set; }
    [Keyword] public string FirstName { get; set; }
    [Keyword] public string LastName { get; set; }
    [Keyword] public string Email { get; set; }
    [Keyword]  public string Picture { get; set; }
    public bool VerifiedEmail { get; set; }
    public bool IsPrivateEmail { get; set; }
    public DateTime AuthTime { get; set; }
}