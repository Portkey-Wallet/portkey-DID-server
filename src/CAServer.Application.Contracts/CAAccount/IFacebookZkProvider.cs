using System.Threading.Tasks;
using CAServer.Verifier.Dtos;

namespace CAServer.CAAccount;

public interface IFacebookZkProvider
{
    public Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto);
}