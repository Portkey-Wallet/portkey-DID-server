using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;

namespace CAServer.CAAccount;

public interface IZkLoginProvider
{
    public bool CanSupportZk(GuardianIdentifierType type);

    public bool CanExecuteZk(GuardianIdentifierType type, ZkLoginInfoRequestDto zkLoginInfo);

    public bool CanExecuteZk(GuardianType type, ZkLoginInfoDto zkLoginInfoDto);
    
    public  Task GenerateGuardianAndUserInfoAsync(GuardianIdentifierType type, string accessToken, string guardianIdentifier, string identifierHash, string salt,
        string chainId = "", string verifierId = "");
}