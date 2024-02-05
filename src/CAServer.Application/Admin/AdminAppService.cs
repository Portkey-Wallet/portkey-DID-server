using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Admin.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.Admin;
using CAServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.Users;

namespace CAServer.Admin;

public class AdminAppService : CAServerAppService, IAdminAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedCache<GoogleTfaCode> _googleMfaCache;
    private readonly IOptionsMonitor<AuthServerOptions> _authServerOptions;

    public AdminAppService(IClusterClient clusterClient, IDistributedCache<GoogleTfaCode> googleMfaCache,
        IOptionsMonitor<AuthServerOptions> authServerOptions)
    {
        _clusterClient = clusterClient;
        _googleMfaCache = googleMfaCache;
        _authServerOptions = authServerOptions;
    }


    public async Task<AdminUserResponse> GetCurrentUserAsync()
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userName = CurrentUser.IsAuthenticated ? CurrentUser.UserName : "noName";
        var roles = CurrentUser.IsAuthenticated
            ? string.Join(CommonConstant.Comma, CurrentUser.Roles)
            : CommonConstant.EmptyString;
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);

        return new AdminUserResponse
        {
            UserId = userId,
            UserName = userName,
            Rules = roles,
            MfaExists = await userMfaGrain.MfaExists()
        };
    }

    public MfaResponse GenerateRandomMfa()
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userName = CurrentUser.IsAuthenticated ? CurrentUser.UserName : "noName";
        var setupCode = GoogleTfaHelper.GenerateGoogleAuthCode(
            RsaHelper.ConvertPrivateKeyToDer(RsaHelper.GenerateRsaKeyPair().Private).ToHex(),
            userName, "CAServer_admin");
        _googleMfaCache.Set(userId.ToString(), setupCode,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
            });
        return new MfaResponse
        {
            CodeImage = setupCode.QrCodeSetupImageUrl,
            ManualEntryKey = setupCode.ManualEntryKey
        };
    }

    public async Task SetMfaAsync(MfaRequest mfaRequest)
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);

        var setupCode = await _googleMfaCache.GetAsync(userId.ToString());
        AssertHelper.NotNull(setupCode, "Code expired");
        AssertHelper.IsTrue(GoogleTfaHelper.VerifyOrderExportCode(mfaRequest.NewPin, setupCode.SourceKey),
            "Invalid new pin");
        AssertHelper.IsTrue(await userMfaGrain.VerifyGoogleTfaPin(mfaRequest.OldPin, true));
        await userMfaGrain.SetMfaAsync(mfaRequest.OldPin, mfaRequest.NewPin, setupCode.SourceKey);
    }

    public async Task ClearMfaAsync(Guid userId)
    {
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);
        await userMfaGrain.ClearMftAsync();
    }

    public async Task AssertMfaAsync(string pin)
    {
        if (_authServerOptions.CurrentValue.DebugMod) return;
        
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);
        AssertHelper.IsTrue(await userMfaGrain.VerifyGoogleTfaPin(pin), "Invalid TFA code");
    }
}