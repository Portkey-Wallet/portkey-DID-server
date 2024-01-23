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
    Task<GrowthIndex> GetGrowthInfoByCaHashAsync(string caHash);
    Task<List<GrowthIndex>> GetGrowthInfosAsync(List<string> caHashes,List<string> inviteCodes);
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

    public async Task<GrowthIndex> GetGrowthInfoByCaHashAsync(string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.CaHash).Value(caHash))
        };

        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _growthRepository.GetAsync(Filter);
    }

    public async Task<List<GrowthIndex>> GetGrowthInfosAsync(List<string> caHashes,List<string> inviteCodes)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>();

        if (!inviteCodes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.InviteCode).Terms(inviteCodes)));
        }
        
        if (!caHashes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHashes)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, data) = await _growthRepository.GetListAsync(Filter);
        return data;
    }
}