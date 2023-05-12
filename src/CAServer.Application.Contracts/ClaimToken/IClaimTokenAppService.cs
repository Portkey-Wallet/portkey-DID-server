using System.Threading.Tasks;
using CAServer.ClaimToken.Dtos;

namespace CAServer.ClaimToken;

public interface IClaimTokenAppService
{
    Task<ClaimTokenResponseDto> GetClaimTokenAsync(ClaimTokenRequestDto claimTokenRequestDto);
}