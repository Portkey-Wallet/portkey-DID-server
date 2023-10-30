using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
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
    Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(string type, string fiat);
    Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(string type, string crypto);
    Task<CommonResponseDto<RampLimitDto>> GetRampLimitAsync(RampLimitRequest request);
    Task<CommonResponseDto<RampExchangeDto>> GetRampExchangeAsync(RampExchangeRequest request);
    Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request);
    Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<Empty>> TransactionForwardCallAsync(TransactionDto input);
    Task<CommonResponseDto<RampFreeLoginDto>> GetRampThirdPartFreeLoginTokenAsync(RampFreeLoginRequest input);
    Task<CommonResponseDto<AlchemySignatureResultDto>> GetRampThirdPartSignatureAsync(RampSignatureRequest input);
    
}