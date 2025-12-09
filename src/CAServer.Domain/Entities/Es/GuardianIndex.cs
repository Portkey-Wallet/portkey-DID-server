using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class GuardianIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public string Identifier { get; set; }
    [Keyword] public string IdentifierHash { get; set; }
    [Keyword] public string OriginalIdentifier { get; set; }
    [Keyword] public string Salt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    
    [Keyword] public string IdentifierPoseidonHash { get; set; }
    
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string SecondaryEmail { get; set; }
}