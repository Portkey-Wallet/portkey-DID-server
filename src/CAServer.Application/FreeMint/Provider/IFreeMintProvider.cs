using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.FreeMint.Provider;

public interface IFreeMintProvider
{
    Task<FreeMintIndex> GetFreeMintItemAsync(string itemId);

    Task<List<FreeMintIndex>> ListFreeMintItemsAsync(List<string> symbols);

    Task<List<FreeMintIndex>> ListFreeMintItemsBySymbolAsync(string symbol);
}

public class FreeMintProvider : IFreeMintProvider, ISingletonDependency
{
    private readonly INESTRepository<FreeMintIndex, string> _freeMintRepository;

    public FreeMintProvider(INESTRepository<FreeMintIndex, string> freeMintRepository)
    {
        _freeMintRepository = freeMintRepository;
    }

    public async Task<FreeMintIndex> GetFreeMintItemAsync(string itemId)
    {
        return await _freeMintRepository.GetAsync(itemId);
    }
    
    public async Task<List<FreeMintIndex>> ListFreeMintItemsAsync(List<string> symbols)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<FreeMintIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Terms(i => i.Field(f => f.Symbol).Terms(symbols)));
        QueryContainer Filter(QueryContainerDescriptor<FreeMintIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, records) = await _freeMintRepository.GetListAsync(Filter);
        return records;
    }
    
    public async Task<List<FreeMintIndex>> ListFreeMintItemsBySymbolAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<FreeMintIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        QueryContainer Filter(QueryContainerDescriptor<FreeMintIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, records) = await _freeMintRepository.GetListAsync(Filter);
        return records;
    }
}