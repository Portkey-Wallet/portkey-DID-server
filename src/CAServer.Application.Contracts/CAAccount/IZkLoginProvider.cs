using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Guardian;

namespace CAServer.CAAccount;

public interface IZkLoginProvider
{
    public bool CanSupportZk(GuardianIdentifierType type);

    public bool CanExecuteZk(GuardianIdentifierType type, ZkLoginInfoRequestDto zkLoginInfo);

    public bool CanExecuteZkByContractZk(Portkey.Contracts.CA.ZkLoginInfo zkLoginInfo);

    public bool CanExecuteZkByZkLoginInfoDto(GuardianType type, ZkLoginInfoDto zkLoginInfoDto);
    
    public Task GenerateGuardianAndUserInfoAsync(GuardianIdentifierType type, string accessToken, string guardianIdentifier, string identifierHash, string salt,
        string chainId = "", string verifierId = "");

    public Task<GuardianEto> UpdateGuardianAsync(string guardianIdentifier, string salt, string identifierHash);
}