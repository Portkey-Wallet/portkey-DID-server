using AElf;
using CAServer.Common;
using Google.Authenticator;
using Microsoft.IdentityModel.Tokens;

namespace CAServer.Commons;

public class GoogleTfaHelper
{
    
    /// <summary>
    ///     Generate a google auth code by key
    /// </summary>
    public static GoogleTfaCode GenerateGoogleAuthCode(string key, string userName, string accountTitle)
    {
        const string defaultName = "noName";
        const string defaultTitle = "CAServer";
        AssertHelper.NotNull(key, "Param key required");
        var tfa = new TwoFactorAuthenticator();
        var tfaCode = new GoogleTfaCode(tfa.GenerateSetupCode( 
            accountTitle.DefaultIfEmpty(defaultTitle),
            userName.DefaultIfEmpty(defaultName),
            HashHelper.ComputeFrom(key).ToByteArray(), 
            5));
        tfaCode.SourceKey = key;
        return tfaCode;
    }

    /// <summary>
    ///     Verify order export auth by google-auth-pin
    /// </summary>
    public static bool VerifyOrderExportCode(string pin, string entryKey)
    {
        if (pin.IsNullOrEmpty() || entryKey.IsNullOrEmpty())
            return false;
        var tfa = new TwoFactorAuthenticator();
        return tfa.ValidateTwoFactorPIN(HashHelper.ComputeFrom(entryKey).ToByteArray(),
            pin);
    }
}

public class GoogleTfaCode {

    public string Account { get; set; }
    public string ManualEntryKey { get; set; }
    
    /// <summary>
    /// Base64-encoded PNG image
    /// </summary>
    public string QrCodeSetupImageUrl { get; set; }
    
    public string SourceKey { get; set; }

    public GoogleTfaCode()
    {
        
    }
    
    public GoogleTfaCode(SetupCode setupCode)
    {
        Account = setupCode.Account;
        ManualEntryKey = setupCode.ManualEntryKey;
        QrCodeSetupImageUrl = setupCode.QrCodeSetupImageUrl;
    }
    
}