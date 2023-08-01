using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyOrderAppService
{
    Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input);

    Task UpdateAlchemyTxHashAsync(TransactionHashDto input);
    Task TransactionAsync(TransactionDto input);
    Task<QueryAlchemyOrderInfo> QueryAlchemyOrderInfoAsync(OrderDto request);
}