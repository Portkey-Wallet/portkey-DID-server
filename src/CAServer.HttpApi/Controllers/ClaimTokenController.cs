using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.ClaimToken;
using CAServer.ClaimToken.Dtos;
using CAServer.Google;
using CAServer.Verifier;
using Microsoft.AspNetCore.Mvc;
using NUglify.Helpers;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ClaimToken")]
[Route("api/app/claimToken")]
public class ClaimTokenController : CAServerController
{
    private readonly IClaimTokenAppService _claimTokenAppService;
    private readonly IGoogleAppService _googleAppService;


    public ClaimTokenController(IClaimTokenAppService claimTokenAppService, IGoogleAppService googleAppService)
    {
        _claimTokenAppService = claimTokenAppService;
        _googleAppService = googleAppService;
    }

    [HttpPost("getClaimToken")]
    public async Task<ClaimTokenResponseDto> ClaimToken([FromHeader] string recaptchaToken,
        ClaimTokenRequestDto claimTokenRequestDto)
    {
        if (recaptchaToken.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Please try again");
        }

        var isGoogleRecaptchaTokenValid =
            await _googleAppService.IsGoogleRecaptchaTokenValidAsync(recaptchaToken, PlatformType.WEB);
        if (isGoogleRecaptchaTokenValid)
        {
            return await _claimTokenAppService.GetClaimTokenAsync(claimTokenRequestDto);
        }

        throw new UserFriendlyException("Please try again");
    }
}