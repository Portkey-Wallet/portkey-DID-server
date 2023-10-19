using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;

namespace CAServer.ThirdPart;

public interface IRampService
{
    
    IThirdPartRampOrderProcessor GetOrderProcessor(string thirdPartName);

    Task<CommonResponseDto<RampCoverage>> GetRampCoverageAsync(string type);

    Task<CommonResponseDto<RampDetail>> GetRampDetailAsync(RampDetailRequest request);

    Task<CommonResponseDto<RampProviderDetail>> GetRampProvidersDetailAsync(RampDetailRequest request);

}