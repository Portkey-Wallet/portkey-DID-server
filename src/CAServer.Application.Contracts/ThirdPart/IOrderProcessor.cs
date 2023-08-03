using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IOrderProcessor
{
    public string MerchantName();
    
    public Task UpdateTxHashAsync(TransactionHashDto transactionHashDto);

    public Task<OrderDto> QueryThirdOrderAsync(OrderDto orderDto);

    public Task<BasicOrderResult> OrderUpdate(IThirdPartOrder thirdPartOrder);

    public Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);

    public Task ForwardTransactionAsync(TransactionDto input);
}