using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyOrderAppService
{
    Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input);
    Task UpdateAlchemyTxHashAsync(UpdateAlchemyTxHashDto input);
}