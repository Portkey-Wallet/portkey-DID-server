using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart.Provider;

public interface IThirdPartOrderProvider
{
    public Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId);
    public Task<OrderDto> GetThirdPartOrderAsync(string orderId);
    public Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync();
    public Task<PageResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] withSections);
    public Task<PageResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition);

    public void SignMerchantDto(NftMerchantBaseDto input);
    public void VerifyMerchantSignature(NftMerchantBaseDto input);
}