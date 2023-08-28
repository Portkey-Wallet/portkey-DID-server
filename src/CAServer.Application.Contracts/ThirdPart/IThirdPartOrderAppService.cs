using System.Threading.Tasks;
using AutoResponseWrapper.Response;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderAppService
{
    Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input);
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    Task<ResponseDto> CreateNftOrderAsync(CreateNftOrderRequestDto input);
    Task<ResponseDto> QueryMerchantNftOrderAsync(OrderQueryRequestDto input);
    Task<ResponseDto> NoticeNftReleaseResultAsync(NftResultRequestDto input);

}

