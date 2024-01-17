using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.Growth.Provider;

public interface IGrowthProvider
{
    Task<GrowthIndex> GetGrowthInfoByLinkCodeAsync(string shortLinkCode);
}

public class GrowthProvider : IGrowthProvider, ISingletonDependency
{
    private readonly INESTRepository<GrowthIndex, string> _growthRepository;

    public GrowthProvider(INESTRepository<GrowthIndex, string> growthRepository)
    {
        _growthRepository = growthRepository;
    }

    public async Task<GrowthIndex> GetGrowthInfoByLinkCodeAsync(string shortLinkCode)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ShortLinkCode).Value(shortLinkCode))
        };

        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _growthRepository.GetAsync(Filter);
    }
}