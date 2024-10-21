using System.Threading.Tasks;

namespace CAServer.Chain;

public interface IChainAppService
{
    Task<ChainResultDto> CreateAsync(CreateUpdateChainDto input);
    Task<ChainResultDto> UpdateAsync(string id, CreateUpdateChainDto input);
    Task DeleteAsync(string chainId);
}