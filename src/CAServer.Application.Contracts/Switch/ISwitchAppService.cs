using System.Threading.Tasks;
using CAServer.Switch.Dtos;

namespace CAServer.Switch;

public interface ISwitchAppService
{
    Task<SwitchDto> GetSwitchStatus(string switchName);
    Task GetSwitchStatus2();
}