using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class UserMarketTokenFavoritesIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    
    
}