using System;
using System.Linq;
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

    public SwitchAppService(IOptionsSnapshot<SwitchOptions> options)
    {
        _options = options.Value;
    }

    public SwitchDto GetSwitchStatus(string switchName)
    {
        var propertyInfo = _options?.GetType().GetProperties().Where(t => t.Name.ToLower() == switchName.ToLower())?.FirstOrDefault();

        if (propertyInfo == null)
        {
            throw new UserFriendlyException("Switch not exists");
        }

        var val = propertyInfo.GetValue(_options);

        return new SwitchDto { IsOpen = Convert.ToBoolean(val) };
    }
}