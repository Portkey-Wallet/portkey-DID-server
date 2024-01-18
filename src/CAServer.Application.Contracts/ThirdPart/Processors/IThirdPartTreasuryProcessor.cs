using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.ThirdPart;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartTreasuryProcessor
{
    public ThirdPartNameType ThirdPartName();

    public Task<TreasuryBaseResult> GetPriceAsync<TPriceInput>(TPriceInput priceInput)
        where TPriceInput : TreasuryBaseContext;

    public Task NotifyOrder<TOrderInput>(TOrderInput orderInput) where TOrderInput : TreasuryBaseContext;
}