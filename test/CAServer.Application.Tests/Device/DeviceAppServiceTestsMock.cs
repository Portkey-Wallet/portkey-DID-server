using CAServer.Options;
using Microsoft.Extensions.Options;
using Orleans;

namespace CAServer.Device;

public partial class DeviceAppServiceTests
{
    private IOptions<DeviceOptions> GetDeviceOptions()
    {
        return new OptionsWrapper<DeviceOptions>(
            new DeviceOptions
            {
                Key = "12345678901234567890123456789012"
            });
    }
}