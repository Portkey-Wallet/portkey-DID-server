using System.Threading.Tasks;
using Portkey.Contracts.CA;

namespace CAServer.Guardian.Provider;

public interface IGuardianProvider
{
    Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash);

    Task<GetHolderInfoOutput> GetHolderInfoFromContractAsync(string guardianIdentifierHash, string caHash,
        string chainId);
}