using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons.Etos;

namespace CAServer.Chain;

public interface IChainAppService
{
    Task<ChainResultDto> CreateAsync(CreateUpdateChainDto input);
    Task<ChainResultDto> UpdateAsync(string id, CreateUpdateChainDto input);
    Task DeleteAsync(string chainId);

    Task<Dictionary<string, ChainDisplayNameDto>> ListChainDisplayInfos(string chainId);
}