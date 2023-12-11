using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Google.Authenticator;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    Task<PagedResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<List<OrderDto>> ExportOrderListAsync(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] orderSectionEnums);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    Task<OrderSettlementGrainDto> GetOrderSettlementAsync(Guid orderId);
    Task AddUpdateOrderSettlementAsync(OrderSettlementGrainDto grainDto);
    SetupCode GenerateGoogleAuthCode(string key, string userName, string accountTitle);
    bool VerifyOrderExportCode(string pin);
}