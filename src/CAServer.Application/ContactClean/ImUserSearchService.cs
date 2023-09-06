using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Search;

namespace CAServer.ContactClean;

public class ImUserSearchService : SearchService<UserIndex, Guid>
{
    public ImUserSearchService(INESTRepository<UserIndex, Guid> nestRepository) : base(nestRepository)
    {
    }
    
    public override string IndexName => "im.userindex";
}