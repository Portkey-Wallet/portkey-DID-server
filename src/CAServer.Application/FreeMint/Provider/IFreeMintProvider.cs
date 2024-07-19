using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using Volo.Abp.DependencyInjection;

namespace CAServer.FreeMint.Provider;

public interface IFreeMintProvider
{
    Task<FreeMintIndex> GetFreeMintItemAsync(string itemId);
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
}