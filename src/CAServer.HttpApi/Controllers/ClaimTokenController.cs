using System.Threading.Tasks;
using CAServer.ClaimToken;
using CAServer.ClaimToken.Dtos;
using CAServer.Google;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ClaimToken")]
[Route("api/app/claim_token")]
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
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            throw new UserFriendlyException("");
        }

        var isGoogleRecaptchaTokenValid = await _googleAppService.IsGoogleRecaptchaTokenValidAsync(recaptchaToken);
        if (isGoogleRecaptchaTokenValid)
        {
            return await _claimTokenAppService.GetClaimTokenAsync(claimTokenRequestDto);
        }

        throw new UserFriendlyException("Validate Failed");
    }
}