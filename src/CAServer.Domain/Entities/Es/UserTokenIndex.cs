using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using Token = CAServer.Tokens.Token;

namespace CAServer.Entities.Es;

public class UserTokenIndex : CAServerEntity<Guid>, IIndexBuild
{
    [Keyword]
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
}