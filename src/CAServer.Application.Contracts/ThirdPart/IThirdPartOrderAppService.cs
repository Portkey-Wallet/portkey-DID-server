using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<CommonResponseDto<CreateNftOrderResponseDto>> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<CommonResponseDto<NftOrderQueryResponseDto>> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    
}