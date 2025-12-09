using System.Threading.Tasks;
using CAServer.Device.Dtos;

namespace CAServer.Device;

public interface IDeviceAppService
{
    Task<DeviceServiceResultDto> EncryptDeviceInfoAsync(DeviceServiceDto serviceDto);
    Task<DeviceServiceResultDto> DecryptDeviceInfoAsync(DeviceServiceDto serviceDto);
    Task<string> EncryptExtraDataAsync(string extraData, string str);
}