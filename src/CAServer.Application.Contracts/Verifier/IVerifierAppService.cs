using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Verifier.Dtos;

namespace CAServer.Verifier;

public interface IVerifierAppService
{
    public Task<VerifierServerResponse> SendVerificationRequestAsync(SendVerificationRequestInput input);

    public Task<VerificationCodeResponse> VerifyCodeAsync(VerificationSignatureRequestDto signatureRequestDto);
    public Task<VerificationCodeResponse> VerifyGoogleTokenAsync(VerifyTokenRequestDto requestDto);
    public Task<VerificationCodeResponse> VerifyAppleTokenAsync(VerifyTokenRequestDto requestDto);
    public Task<long> CountVerifyCodeInterfaceRequestAsync(string userIpAddress);
}