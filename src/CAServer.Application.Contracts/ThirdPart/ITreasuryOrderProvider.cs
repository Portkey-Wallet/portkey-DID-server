using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Dtos.Order;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart;

public interface ITreasuryOrderProvider : ITransientDependency
{
    Task<TreasuryOrderDto> DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null);
    
    Task<PendingTreasuryOrderDto> AddOrUpdatePendingTreasuryOrder(PendingTreasuryOrderDto pendingTreasuryOrderDto);
    
    Task<PagedResultDto<TreasuryOrderDto>> QueryOrderAsync(TreasuryOrderCondition condition);

    Task<PagedResultDto<OrderStatusInfoIndex>> QueryOrderStatusInfoPagerAsync(List<string> ids);
    
    Task<PagedResultDto<PendingTreasuryOrderDto>> QueryPendingTreasuryOrder(PendingTreasuryOrderCondition condition);

}