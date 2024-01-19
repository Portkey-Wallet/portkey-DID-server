using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.ThirdPart;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartTreasuryProcessor
{
    ThirdPartNameType ThirdPartName();

    Task<TreasuryBaseResult> GetPriceAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext;

    Task NotifyOrderAsync<TOrderInput>(TOrderInput orderInput) where TOrderInput : TreasuryBaseContext;

    Task CallBackAsync(Guid orderId);
}