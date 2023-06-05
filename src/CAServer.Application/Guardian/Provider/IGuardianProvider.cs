using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using Portkey.Contracts.CA;

namespace CAServer.Guardian.Provider;

public interface IGuardianProvider
{
    Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash);

    Task<GetHolderInfoOutput> GetHolderInfoFromContractAsync(string guardianIdentifierHash, string caHash,
        string chainId);
}