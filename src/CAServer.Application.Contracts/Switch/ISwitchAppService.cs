using CAServer.Switch.Dtos;

namespace CAServer.Switch;

public interface ISwitchAppService
{
    SwitchDto GetSwitchStatus(string switchName);
}