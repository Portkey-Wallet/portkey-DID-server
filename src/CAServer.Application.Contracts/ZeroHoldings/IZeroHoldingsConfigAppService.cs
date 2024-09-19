using System.Threading.Tasks;
using CAServer.ZeroHoldings.Dtos;

namespace CAServer.ZeroHoldings;

public interface IZeroHoldingsConfigAppService
{
    Task<bool> SetStatus(ZeroHoldingsConfigDto status);

    Task<ZeroHoldingsConfigDto> GetStatus();
}