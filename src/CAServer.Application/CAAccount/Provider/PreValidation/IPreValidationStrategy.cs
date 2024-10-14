using System.Threading.Tasks;
using CAServer.Account;
using CAServer.Dtos;

namespace CAServer.CAAccount.Provider;

public interface IPreValidationStrategy
{
    public PreValidationType Type { get; }
    
    public bool ValidateParameters(GuardianInfo guardian);

    public Task<bool> PreValidateGuardian(string chainId, string caHash, string manager, GuardianInfo guardian);
}