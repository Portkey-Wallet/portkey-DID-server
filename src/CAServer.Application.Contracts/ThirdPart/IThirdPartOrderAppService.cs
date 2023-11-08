using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Google.Authenticator;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<List<OrderDto>> ExportOrderList(GetThirdPartOrderConditionDto condition);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    public SetupCode GenerateOrderListSetupCode(string key, string userName, string accountTitle);
    bool VerifyOrderListCode(string pin);
}