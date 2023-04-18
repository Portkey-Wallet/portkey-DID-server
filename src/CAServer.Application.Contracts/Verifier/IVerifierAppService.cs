using System.Threading.Tasks;
using CAServer.Dtos;

namespace CAServer.Verifier;

public interface IVerifierAppService
{
    public Task<VerifierServerResponse> SendVerificationRequestAsync(SendVerificationRequestInput input);

    public Task<VerificationCodeResponse> VerifyCodeAsync(VerificationSignatureRequestDto signatureRequestDto);

}