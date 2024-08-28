using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Verifier.Dtos;

namespace CAServer.Verifier;

public interface IVerifierServerClient
{
    Task<ResponseResultDto<VerifierServerResponse>> SendVerificationRequestAsync(VerifierCodeRequestDto dto);

    Task<ResponseResultDto<VerifierServerResponse>> SendSecondaryEmailVerificationRequestAsync(
        string secondaryEmail, string verifierSessionId);

    Task<ResponseResultDto<VerifierServerResponse>> SendNotificationBeforeApprovalAsync(
        VerifierCodeRequestDto dto);
    Task<ResponseResultDto<VerificationCodeResponse>> VerifyCodeAsync(VierifierCodeRequestInput input);

    Task<ResponseResultDto<bool>> VerifySecondaryEmailCodeAsync(string verifierSessionId,
        string verificationCode, string secondaryEmail, string verifierEndpoint);

    Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt);

    Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt);

    Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt);

    Task<ResponseResultDto<VerificationCodeResponse>> VerifyFacebookTokenAsync(VerifyTokenRequestDto requestDto, string identifierHash, string salt);
    Task<ResponseResultDto<VerifyFacebookUserInfoDto>> VerifyFacebookAccessTokenAsync(VerifyTokenRequestDto requestDto);
    
    Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyTwitterTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt);

    Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeInput input);
}