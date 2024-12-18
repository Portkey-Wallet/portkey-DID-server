using System.Collections.Generic;
using System.Linq;
using CAServer.Commons;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common.AelfClient;

public interface IContractClientSelector
{
    IContractClient GetContractClient(string chainId);
}

public class ContractClientSelector : IContractClientSelector, ISingletonDependency
{
    private readonly IEnumerable<IContractClient> _contractClients;

    public ContractClientSelector(IEnumerable<IContractClient> contractClients)
    {
        _contractClients = contractClients;
    }

    public IContractClient GetContractClient(string chainId)
    {
        return chainId == CommonConstant.MainChainId
            ? _contractClients.FirstOrDefault(t => t.GetType().Name == nameof(MainChainContractClient))
            : _contractClients.FirstOrDefault(t => t.GetType().Name == nameof(SideChainContractClient));
    }
}