using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    
    // order
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    
    // ramp
    Task<CommonResponseDto<RampCoverageDto>> GetRampCoverageAsync();
    Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request);
    Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<Empty>> TransactionForwardCall(TransactionDto input);
    
    Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(string type, string fiat);
    Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(string type, string crypto);
}