using System.Security.Cryptography;
using CAServer.Grains.State.Device;

namespace CAServer.Grains.Grain.Device;

public class DeviceGrain : Grain<DeviceState>, IDeviceGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<string> GetOrGenerateSaltAsync()
    {
        if (State.Salt.IsNullOrWhiteSpace())
        {
            State.Salt = GenerateSalt(DeviceGrainConstants.RandomSaltSize);
            await WriteStateAsync();
        }

        return State.Salt;
    }

    private string GenerateSalt(int size)
    {
        var salt = new byte[size];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        return Convert.ToBase64String(salt).Substring(0, size);
    }

    public async Task SetSaltAsync(string salt)
    {
        State.Salt = salt;
        await WriteStateAsync();
    }
}