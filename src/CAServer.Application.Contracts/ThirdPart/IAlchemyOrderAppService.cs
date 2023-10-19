using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;

namespace CAServer.ThirdPart;

public interface IAlchemyOrderAppService
{
    Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input);

    Task UpdateAlchemyTxHashAsync(SendAlchemyTxHashDto input);
    Task TransactionAsync(TransactionDto input);
    Task<QueryAlchemyOrderInfo> QueryAlchemyOrderInfoAsync(OrderDto request);
}