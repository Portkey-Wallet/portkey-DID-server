using System.Threading.Tasks;
using CAServer.Verifier.Dtos;

namespace CAServer.CAAccount;

public interface IAppleZkProvider
{
    public Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto);

    public Task<AppleUserExtraInfo> GetAppleUserExtraInfo(string accessToken);
}