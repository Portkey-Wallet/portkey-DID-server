using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Verifier.Dtos;

namespace CAServer.Verifier;

public interface IVerifierServerClient
{
    Task<ResponseResultDto<VerifierServerResponse>> SendVerificationRequestAsync(VerifierCodeRequestDto dto);
    Task<ResponseResultDto<VerificationCodeResponse>> VerifyCodeAsync(VierifierCodeRequestInput input);
}