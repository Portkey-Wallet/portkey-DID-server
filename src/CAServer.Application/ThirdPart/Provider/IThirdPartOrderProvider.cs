using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos;
using JetBrains.Annotations;

namespace CAServer.ThirdPart.Provider;

public interface IThirdPartOrderProvider
{
    public Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId);
    public Task<OrderDto> GetThirdPartOrderAsync(string orderId);
    public Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync();
    public Task<PageResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, List<Guid> orderIdIn, int skipCount, int maxResultCount, params OrderSectionEnum?[] withSections);
    public Task<PageResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition);
}