using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Device;
using CAServer.Device.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAServer.Controllers;

[Area("app")]
[ControllerName("Device")]
[Route("api/app/account/")]
[Authorize]
public class DeviceController
{
    private readonly IDeviceAppService _deviceAppService;

    public DeviceController(IDeviceAppService deviceAppService)
    {
        _deviceAppService = deviceAppService;
    }

    [HttpPost("device/encrypt")]
    public async Task<DeviceServiceResultDto> EncryptDeviceInfoAsync([FromBody] DeviceServiceDto input)
    {
        return await _deviceAppService.EncryptDeviceInfoAsync(input);
    }

    [HttpPost("device/decrypt")]
    public async Task<DeviceServiceResultDto> DecryptDeviceInfoAsync([FromBody] DeviceServiceDto input)
    {
        return await _deviceAppService.DecryptDeviceInfoAsync(input);
    }
}