using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IOrderProcessor
{
    public string MerchantName();

    public Task UpdateTxHashAsync(TransactionHashDto transactionHashDto);

    public Task<T> QueryThirdOrder<T>(T orderDto) where T : OrderDto;

    public Task<BasicOrderResult> OrderUpdate<T>(T input) where T : OrderDto;

    public Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);

    public Task<OrdersDto> GetThirdPartOrdersAsync(GetUserOrdersDto input);

    public Task ForwardTransactionAsync(TransactionDto input);
}