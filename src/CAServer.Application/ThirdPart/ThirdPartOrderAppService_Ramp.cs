using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppService
{

    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    
    public Task<CommonResponseDto<RampCoverageDto>> GetRampCoverageAsync()
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<Empty>> TransactionForwardCall(TransactionDto input)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(string type, string fiat)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(string type, string crypto)
    {
        throw new NotImplementedException();
    }
}