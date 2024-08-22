using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Guardian;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;

namespace CAServer.CAAccount;

public interface IZkLoginProvider
{
    public bool CanSupportZk(GuardianIdentifierType type);

    public bool CanExecuteZk(GuardianIdentifierType type, ZkLoginInfoRequestDto zkLoginInfo);
    
    public bool CanExecuteZkByZkLoginInfoDto(GuardianType type, ZkLoginInfoDto zkLoginInfoDto);
    
    public Task<GuardianEto> UpdateGuardianAsync(string guardianIdentifier, string salt, string identifierHash);
    
    public Task<VerifiedZkResponse> VerifiedZkLoginAsync(VerifiedZkLoginRequestDto requestDto);
    
    Task<CAHolderReponse> GetAllCaHolderWithTotalAsync(int skip, int limit);

    Task<GuardiansAppDto> GetCaHolderInfoAsync(int skip, int limit);

    public Task AppendSinglePoseidonAsync(AppendSinglePoseidonDto request);
}