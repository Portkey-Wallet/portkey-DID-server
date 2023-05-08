using System.Threading.Tasks;
using CAServer.ClaimToken.Dtos;
using CAServer.Common;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.ClaimToken;

public class ClaimTokenAppService : IClaimTokenAppService, ISingletonDependency
{

    private ClaimTokenWhiteListAddressesOptions _claimTokenWhiteListAddressesOptions;
    private IContractProvider _contractProvider;

    public ClaimTokenAppService(IOptionsSnapshot<ClaimTokenWhiteListAddressesOptions> claimTokenWhiteListAddressesOptions, IContractProvider contractProvider)
    {
        _contractProvider = contractProvider;
        _claimTokenWhiteListAddressesOptions = claimTokenWhiteListAddressesOptions.Value;
    }

    public async Task<ClaimTokenResponseDto> GetClaimTokenAsync(ClaimTokenRequestDto claimTokenRequestDto)
    {
        

        return new ClaimTokenResponseDto();
    }
}