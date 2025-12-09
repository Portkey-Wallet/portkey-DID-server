using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.RedDot.Dtos;

namespace CAServer.RedDot;

public interface IRedDotAppService
{
    Task<RedDotInfoDto> GetRedDotInfoAsync(RedDotType redDotType);
    Task SetRedDotAsync(RedDotType redDotType);
}