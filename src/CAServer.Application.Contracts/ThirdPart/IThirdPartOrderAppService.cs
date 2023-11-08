using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    
    // order
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<string>> InitOrderAsync(Guid orderId, Guid userId);
    Task<CommonResponseDto<Empty>> OrderUpdateAsync(string thirdPart, IThirdPartOrder thirdPartOrder);
    Task UpdateOffRampTxHash(TransactionHashDto input);
    Task<CommonResponseDto<OrderDto>> QueryThirdPartRampOrder(OrderDto orderDto);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    
    // ramp
    Task<CommonResponseDto<RampCoverageDto>> GetRampCoverageAsync();
    Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(RampCryptoRequest rampCryptoRequest);
    Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(RampFiatRequest request);
    Task<CommonResponseDto<RampLimitDto>> GetRampLimitAsync(RampLimitRequest request);
    Task<CommonResponseDto<RampExchangeDto>> GetRampExchangeAsync(RampExchangeRequest request);
    Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request);
    Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<Empty>> TransactionForwardCallAsync(TransactionDto input);
    
}