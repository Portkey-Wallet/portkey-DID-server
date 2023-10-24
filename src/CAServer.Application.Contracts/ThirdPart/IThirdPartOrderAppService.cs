using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    
    // order
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    
    // ramp
    Task<CommonResponseDto<RampCoverage>> GetRampCoverageAsync(string type);
    Task<CommonResponseDto<RampDetail>> GetRampDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<RampProviderDetail>> GetRampProvidersDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<Empty>> TransactionForwardCall(TransactionDto input);
}