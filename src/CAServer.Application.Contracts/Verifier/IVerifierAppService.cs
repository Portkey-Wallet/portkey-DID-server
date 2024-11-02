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
    public Task<bool> GuardianExistsAsync(string guardianIdentifier);
    public Task<GetVerifierServerResponse> GetVerifierServerAsync(string chainId);
    public Task<VerificationCodeResponse> VerifyTelegramTokenAsync(VerifyTokenRequestDto requestDto);
    public Task<VerificationCodeResponse> VerifyFacebookTokenAsync(VerifyTokenRequestDto requestDto);
    Task<VerificationCodeResponse> VerifyTwitterTokenAsync(VerifyTokenRequestDto requestDto);
    Task<CAHolderResultDto> GetHolderInfoByCaHashAsync(string caHash);
    Task<VerifierServersBasicInfoResponse> GetVerifierServerDetailsAsync(string chainId);
    Task RemoveVerifierServerDetailsCacheAsync(string chainId);
}