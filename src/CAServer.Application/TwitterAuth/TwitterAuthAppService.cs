using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Signature.Provider;
using CAServer.TwitterAuth.Dtos;
using CAServer.TwitterAuth.Etos;
using CAServer.TwitterAuth.Provider;
using CAServer.Verifier.Etos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.TwitterAuth;

[RemoteService(false), DisableAuditing]
public class TwitterAuthAppService : CAServerAppService, ITwitterAuthAppService
{
    private readonly IHttpClientService _httpClientService;
    private readonly TwitterAuthOptions _options;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecretProvider _secretProvider;
    private readonly ITwitterAuthProvider _twitterAuthProvider;

    public TwitterAuthAppService(IHttpClientService httpClientService, IOptionsSnapshot<TwitterAuthOptions> options,
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IHttpContextAccessor httpContextAccessor, ISecretProvider secretProvider,
        ITwitterAuthProvider twitterAuthProvider)
    {
        _httpClientService = httpClientService;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpContextAccessor = httpContextAccessor;
        _secretProvider = secretProvider;
        _twitterAuthProvider = twitterAuthProvider;
        _options = options.Value;
    }

    public async Task<TwitterAuthResultDto> ReceiveAsync(TwitterAuthDto twitterAuthDto)
    {
        var authResult = new TwitterAuthResultDto();
        try
        {
            var authUserInfo = await ValidAuthCodeAsync(twitterAuthDto);
            authResult.Data = authUserInfo;
            return authResult;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "valid twitter code error, code:{code}", twitterAuthDto.Code ?? string.Empty);
            var errorInfo = GetErrorInfo(e);
            authResult.Code = errorInfo.code;
            authResult.Message = errorInfo.message;

            Logger.LogInformation("twitter auth result error info, code:{code},message:{message}",
                authResult.Code ?? string.Empty,
                authResult.Message ?? string.Empty);
            return authResult;
        }
    }

    private async Task<TwitterUserAuthInfoDto> ValidAuthCodeAsync(TwitterAuthDto twitterAuthDto)
    {
        var clientSecret = await _secretProvider.GetSecretWithCacheAsync(_options.ClientId);
        Logger.LogInformation("receive twitter callback, code:{code}, redirectUrl:{redirectUrl}",
            twitterAuthDto.Code ?? string.Empty,
            twitterAuthDto.RedirectUrl ?? string.Empty);

        if (twitterAuthDto.Code.IsNullOrEmpty())
        {
            throw new UserFriendlyException("auth code is empty", AuthErrorMap.TwitterCancelCode);
        }

        var basicAuth = GetBasicAuth(_options.ClientId, clientSecret);
        var requestParam = new Dictionary<string, string>
        {
            ["code"] = twitterAuthDto.Code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = twitterAuthDto.RedirectUrl ??
                               $"{_options.RequestRedirectUrl}{_httpContextAccessor.HttpContext.Request.Path}",
            ["code_verifier"] = "challenge"
        };

        var header = new Dictionary<string, string>
        {
            [CommonConstant.AuthHeader] = basicAuth
        };

        var response = await _httpClientService.PostAsync<TwitterTokenDto>(CommonConstant.TwitterTokenUrl,
            RequestMediaType.Form, requestParam, header);

        Logger.LogInformation("get accessToken from twitter success, response:{response}",
            JsonConvert.SerializeObject(response));

        var userInfo = await SaveUserExtraInfoAsync(response.AccessToken);
        return new TwitterUserAuthInfoDto
        {
            AccessToken = response.AccessToken,
            UserInfo = userInfo.Data
        };
    }

    private string GetBasicAuth(string clientId, string clientSecret)
    {
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        return $"Basic {basicToken}";
    }

    private async Task<TwitterUserInfoDto> SaveUserExtraInfoAsync(string accessToken)
    {
        var header = new Dictionary<string, string>
        {
            [CommonConstant.AuthHeader] = $"{CommonConstant.JwtTokenPrefix} {accessToken}"
        };
        var userInfo = await _twitterAuthProvider.GetUserInfoAsync(CommonConstant.TwitterUserInfoUrl, header);

        if (userInfo == null)
        {
            throw new UserFriendlyException("Failed to get user info");
        }

        // statistic
        await _distributedEventBus.PublishAsync(new TwitterStatisticEto
        {
            Id = userInfo.Data.Id,
            UpdateTime = TimeHelper.GetTimeStampInSeconds()
        });
        
        Logger.LogInformation("get twitter user info success, data:{userInfo}", JsonConvert.SerializeObject(userInfo));
        var userExtraInfo = new Verifier.Dtos.UserExtraInfo
        {
            Id = userInfo.Data.Id,
            FullName = userInfo.Data.UserName,
            FirstName = userInfo.Data.Name,
            GuardianType = GuardianIdentifierType.Twitter.ToString(),
            AuthTime = DateTime.UtcNow
        };

        await AddUserInfoAsync(userExtraInfo);
        return userInfo;
    }

    private async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);

        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAppleUserAsync(
            ObjectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto));
    }

    private (string code, string message) GetErrorInfo(Exception exception)
    {
        string errorCode = AuthErrorMap.DefaultCode;
        if (exception is UserFriendlyException friendlyException)
        {
            errorCode = friendlyException.Code == HttpStatusCode.TooManyRequests.ToString()
                ? AuthErrorMap.TwitterCancelCode
                : friendlyException.Code;
        }

        var message = AuthErrorMap.GetMessage(errorCode);
        return (errorCode, message);
    }
}