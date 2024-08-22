using System.Threading.Tasks;
using CAServer.Verifier.Dtos;

namespace CAServer.CAAccount;

public interface IGoogleZkProvider
{
    public Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto);

    public string GetGoogleAuthRedirectUrl();
}