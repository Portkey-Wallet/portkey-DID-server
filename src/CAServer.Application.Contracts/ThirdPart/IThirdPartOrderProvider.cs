using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart;

public interface IThirdPartOrderProvider
{
    public Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId);
    public Task<Dictionary<Guid, RampOrderIndex>> GetThirdPartOrderIndexAsync(List<string> orderIdIn);
    public Task<OrderDto> GetThirdPartOrderAsync(string orderId);
    public Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync();
    public Task<PagedResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] withSections);
    public Task<PagedResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition);
    public Task<PagedResultDto<NftOrderIndex>> QueryNftOrderPagerAsync(NftOrderQueryConditionDto condition);
    public Task UpdateOrderAsync(OrderDto orderDto);
    public void SignMerchantDto(NftMerchantBaseDto input);
    public void VerifyMerchantSignature(NftMerchantBaseDto input);
}