using System.Threading.Tasks;

namespace CAServer.Guardian.Provider;

public interface IGuardianProvider
{
    Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash);
    Task<string> GetRegisterChainIdAsync(string loginGuardianIdentifierHash, string caHash);
}