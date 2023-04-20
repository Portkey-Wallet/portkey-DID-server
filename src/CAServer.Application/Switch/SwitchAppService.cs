using CAServer.Options;
using CAServer.Switch.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Switch;

[RemoteService(false), DisableAuditing]
public class SwitchAppService : CAServerAppService, ISwitchAppService
{
    private readonly SwitchOptions _options;

    public SwitchAppService(IOptions<SwitchOptions> options)
    {
        _options = options.Value;
    }

    public RampSwitchDto GetSwitchStatus() => new RampSwitchDto { IsOpen = _options.RampSwitch };
}