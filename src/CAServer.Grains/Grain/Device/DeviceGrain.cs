using System.Security.Cryptography;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.State.Device;
using Microsoft.Extensions.Options;
using Orleans;

namespace CAServer.Grains.Grain.Device;

public class DeviceGrain : Grain<DeviceState>, IDeviceGrain
{
    private readonly CAAccountOption _caAccountOption;

    public DeviceGrain(IOptionsSnapshot<CAAccountOption> caAccountOption)
    {
        _caAccountOption = caAccountOption.Value;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<string> GetOrGenerateSaltAsync()
    {
        var caAccountRequestInfoMaxLength = _caAccountOption.CAAccountRequestInfoMaxLength;
        var caAccountRequestInfoExpirationTime = _caAccountOption.CAAccountRequestInfoExpirationTime;
        await Console.Out.WriteAsync(caAccountRequestInfoExpirationTime+"<__>"+caAccountRequestInfoMaxLength);
        
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