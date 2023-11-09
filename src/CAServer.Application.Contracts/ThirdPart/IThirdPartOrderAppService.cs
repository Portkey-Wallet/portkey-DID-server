using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Google.Authenticator;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<List<OrderDto>> ExportOrderList(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] orderSectionEnums);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    Task<OrderSettlementGrainDto> GetOrderSettlementAsync(Guid orderId);
    Task<GrainResultDto<OrderSettlementGrainDto>> AddUpdateOrderSettlementAsync(OrderSettlementGrainDto grainDto);
    public SetupCode GenerateGoogleAuthCode(string key, string userName, string accountTitle);
    bool VerifyOrderExportCode(string pin);
}