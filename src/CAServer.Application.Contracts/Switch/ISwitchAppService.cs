using CAServer.Switch.Dtos;

namespace CAServer.Switch;

public interface ISwitchAppService
{
    RampSwitchDto GetSwitchStatus();
}