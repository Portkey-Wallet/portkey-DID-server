namespace CAServer.Grains.Grain.Device;

public interface IDeviceGrain : IGrainWithStringKey
{
    Task<string> GetOrGenerateSaltAsync();

    Task SetSaltAsync(string salt);
}