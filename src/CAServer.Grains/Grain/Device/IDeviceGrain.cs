using Orleans;

namespace CAServer.Grains.Grain.Device;

public interface IDeviceGrain : IGrainWithStringKey
{
    Task<string> GetOrGenerateSaltAsync();
}