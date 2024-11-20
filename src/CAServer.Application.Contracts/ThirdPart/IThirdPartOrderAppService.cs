using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Admin.Dtos;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
using CAServer.ThirdPart.Dtos.Order;
using Google.Authenticator;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    
    // order
    Task<PagedResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<PagedResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition,
        params OrderSectionEnum?[] withSections);
    Task<List<OrderDto>> ExportOrderListAsync(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] orderSectionEnums);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<string>> InitOrderAsync(Guid orderId, Guid userId);
    Task<CommonResponseDto<Empty>> OrderUpdateAsync(string thirdPart, IThirdPartOrder thirdPartOrder);
    Task UpdateOffRampTxHashAsync(TransactionHashDto input);
    Task<CommonResponseDto<OrderDto>> QueryThirdPartRampOrderAsync(OrderDto orderDto);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderDto orderDto, string reason = null);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    
    // nft
    Task<OrderSettlementGrainDto> GetOrderSettlementAsync(Guid orderId);
    Task AddUpdateOrderSettlementAsync(OrderSettlementGrainDto grainDto);
    bool VerifyOrderExportCode(string pin);
    
    // ramp
    Task<CommonResponseDto<RampCoverageDto>> GetRampCoverageAsync();
    Task<CommonResponseDto<RampCryptoDto>> GetRampCryptoListAsync(RampCryptoRequest rampCryptoRequest);
    Task<(List<TransakCryptoItem>, string)> GetCryptoCurrenciesAsync(RampCryptoRequest request);
    Task<CommonResponseDto<RampFiatDto>> GetRampFiatListAsync(RampFiatRequest request);
    Task<CommonResponseDto<RampLimitDto>> GetRampLimitAsync(RampLimitRequest request);
    Task<CommonResponseDto<RampExchangeDto>> GetRampExchangeAsync(RampExchangeRequest request);
    Task<CommonResponseDto<RampPriceDto>> GetRampPriceAsync(RampDetailRequest request);
    Task<CommonResponseDto<RampDetailDto>> GetRampDetailAsync(RampDetailRequest request);
    Task<CommonResponseDto<Empty>> TransactionForwardCallAsync(TransactionDto input);
    
    // treasury
    Task<CommonResponseDto<Empty>> UpdateTreasuryOrderAsync(TreasuryOrderDto orderDto, string reason = null);
    
}