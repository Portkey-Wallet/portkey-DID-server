using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class CAHolderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string NickName { get; set; }
    public string Avatar { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }

}