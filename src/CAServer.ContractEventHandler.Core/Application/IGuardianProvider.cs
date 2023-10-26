using System.Threading.Tasks;
using CAServer.Guardian.Provider;
using Portkey.Contracts.CA;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IGuardianProvider
{
    Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash);
}

