using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Admin.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.Admin;
using Microsoft.Extensions.Caching.Distributed;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.Users;

namespace CAServer.Admin;

public class AdminAppService : CAServerAppService, IAdminAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedCache<GoogleTfaCode> _googleMfaCache;

    public AdminAppService(IClusterClient clusterClient, IDistributedCache<GoogleTfaCode> googleMfaCache)
    {
        _clusterClient = clusterClient;
        _googleMfaCache = googleMfaCache;
    }


    public async Task<AdminUserResponse> GetCurrentUserAsync()
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userName = CurrentUser.IsAuthenticated ? CurrentUser.UserName : "noName";
        
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);

        return new AdminUserResponse
        {
            UserId = userId,
            UserName = userName,
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

    public async Task SetMfa(MfaRequest mfaRequest)
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

    public async Task ClearMfa(Guid userId)
    {
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);
        await userMfaGrain.ClearMftAsync();
    }

    public async Task AssertMfa(string pin)
    {
        var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
        var userMfaGrain = _clusterClient.GetGrain<IUserMfaGrain>(userId);
        AssertHelper.IsTrue(await userMfaGrain.VerifyGoogleTfaPin(pin));
    }
}