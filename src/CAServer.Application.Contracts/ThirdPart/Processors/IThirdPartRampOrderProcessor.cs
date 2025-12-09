using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartRampOrderProcessor
{
    /// <summary>
    ///     Name of ThirdPart
    ///     <see cref="ThirdPartNameType"/>
    /// </summary>
    /// <returns></returns>
    public string ThirdPartName();

    Task UpdateTxHashAsync(TransactionHashDto transactionHashDto);

    /// <summary>
    ///     Update thirdPart via webhook API
    /// </summary>
    /// <param name="thirdPartOrder"></param>
    /// <returns></returns>
    Task<BasicOrderResult> OrderUpdateAsync(IThirdPartOrder thirdPartOrder);

    /// <summary>
    ///     Create an EMPTY ramp order by user
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<OrderCreatedDto> CreateThirdPartOrderAsync(CreateUserOrderDto input);
    
    /// <summary>
    ///     Send user transaction forward to Node, and bind transactionHash with ramp-order 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task ForwardTransactionAsync(TransactionDto input);


}