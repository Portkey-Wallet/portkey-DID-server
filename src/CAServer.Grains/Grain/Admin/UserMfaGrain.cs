using CAServer.Commons;
using CAServer.Grains.State;

namespace CAServer.Grains.Grain.Admin;

public interface IUserMfaGrain : IGrainWithGuidKey
{
    Task SetMfaAsync(string oldPin, string newPin, string newKey);

    Task<bool> MfaExists();
    
    Task<bool> VerifyGoogleTfaPin(string pin, bool resultIfNotSet = false);

    Task ClearMftAsync();
}

public class UserMfaGrain : Grain<UserMfaState>, IUserMfaGrain
{
    public async Task SetMfaAsync(string oldPin, string newPin, string newKey)
    {
        State.GoogleTwoFactorAuthKey = newKey;
        State.LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds();
        await WriteStateAsync();
    }

    public async Task<bool> MfaExists()
    {
        return State.GoogleTwoFactorAuthKey.NotNullOrEmpty();
    }

    public async Task<bool> VerifyGoogleTfaPin(string pin, bool resultIfNotSet = false)
    {
        if (State.GoogleTwoFactorAuthKey.IsNullOrEmpty()) return resultIfNotSet;
        if (pin.IsNullOrEmpty()) return false;
        return GoogleTfaHelper.VerifyOrderExportCode(pin, State.GoogleTwoFactorAuthKey);
    }

    public async Task ClearMftAsync()
    {
        State.GoogleTwoFactorAuthKey = "";
        await WriteStateAsync();
    }
}