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
using Volo.Abp.Caching;
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
    private readonly IDistributedCache<string> _distributedCache;
    private const string _twitterTokenCache = "TwitterAuth";

    public TwitterAuthAppService(IHttpClientService httpClientService, IOptionsSnapshot<TwitterAuthOptions> options,
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IHttpContextAccessor httpContextAccessor, ISecretProvider secretProvider,
        ITwitterAuthProvider twitterAuthProvider,
         IDistributedCache<string> distributedCache)
    {
        _httpClientService = httpClientService;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpContextAccessor = httpContextAccessor;
        _secretProvider = secretProvider;
        _twitterAuthProvider = twitterAuthProvider;
        _distributedCache = distributedCache;
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

    public async Task<string> LoginAsync()
    {
        var baseUrl = "https://twitter.com/oauth/request_token";
        var callback = "https://test4-applesign-v2.portkey.finance/api/app/twitterAuth/callback";
        var timestamp = TimeHelper.GetTimeStampInSeconds().ToString();
        var nonce = Auth10aHelper.GenerateNonce();
        var signature = Auth10aHelper.GenerateSignature(new Uri(baseUrl), _options.CustomKey, _options.CustomSecret,
            token: string.Empty, tokenSecret: string.Empty,
            httpMethod: "POST", timestamp, nonce, verifier: string.Empty, callback,
            out string normalizedUrl, out string normalizedRequestParameters);

        var authorizationHeaderParams =
            "oauth_callback=\"https%3A%2F%2Ftest4-applesign-v2.portkey.finance%2Fapi%2Fapp%2FtwitterAuth%2Fcallback\"," +
            "oauth_consumer_key=\"" + _options.CustomKey + "\",oauth_nonce=\"" + nonce +
            "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" + timestamp +
            "\",oauth_version=\"1.0A\",oauth_signature=\"" +
            Auth10aHelper.UrlEncode(signature) + "\"";

        var dic = new Dictionary<string, string>
        {
            ["authorization"] = $"OAuth {authorizationHeaderParams}"
        };

        var response = await _twitterAuthProvider.PostFormAsync(baseUrl,
            new Dictionary<string, string>(), dic);

        Logger.LogInformation("response from twitter request token: {token}", response);

        if (!response.Contains("oauth_token") && !response.Contains("oauth_token_secret"))
        {
            throw new UserFriendlyException("twitter auth fail");
        }

        var resList = response.Split('&');

        var oauthToken = resList[0].Substring(resList[0].IndexOf('=') + 1);
        var oauthTokenSecret = resList[1].Substring(resList[1].IndexOf('=') + 1);
        await _distributedCache.SetAsync(_twitterTokenCache + ":" + oauthToken, oauthTokenSecret);
        return oauthToken;
    }

    public async Task<TwitterAuthResultDto> LoginCallBackAsync(string oauthToken, string oauthVerifier)
    {
        Logger.LogInformation("twitter login callback, oauthToken:{oauthToken}, oauthVerifier:{oauthVerifier}",
            oauthToken, oauthVerifier);

        var baseUrl = "https://twitter.com/oauth/access_token";
        var timestamp = TimeHelper.GetTimeStampInSeconds().ToString();
        var nonce = Auth10aHelper.GenerateNonce();

        var tokenSecret = await _distributedCache.GetAsync(_twitterTokenCache + ":" + oauthToken);
        var signature = Auth10aHelper.GenerateSignature(new Uri(baseUrl), _options.CustomKey, _options.CustomSecret,
            token: oauthToken, tokenSecret: tokenSecret,
            httpMethod: "POST", timestamp, nonce, verifier: oauthVerifier, callback: string.Empty,
            out string normalizedUrl, out string normalizedRequestParameters);

        var authorizationHeaderParams =
            "oauth_consumer_key=\"" + _options.CustomKey + "\",oauth_nonce=\"" + nonce +
            "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" + timestamp +
            "\",oauth_token=\"" + oauthToken +
            "\",oauth_version=\"1.0A\"" + ",oauth_verifier=\"" + oauthVerifier + "\",oauth_signature=\"" +
            Auth10aHelper.UrlEncode(signature) + "\"";

        var headers = new Dictionary<string, string>
        {
            ["authorization"] = $"OAuth {authorizationHeaderParams}"
        };

        var response = await _twitterAuthProvider.PostFormAsync(baseUrl, new Dictionary<string, string>(), headers);

        Logger.LogInformation("get twitter access token success: {token}", response);

        var resList = response.Split('&');

        if (!response.Contains("oauth_token") && !response.Contains("oauth_token_secret"))
        {
            throw new UserFriendlyException("twitter auth fail");
        }

        var oauthAccessToken = resList[0].Substring(resList[0].IndexOf('=') + 1);
        var oauthTokenSecret = resList[1].Substring(resList[1].IndexOf('=') + 1);

        var userInfo = await SaveAuthUserExtraInfoAsync(oauthAccessToken, oauthTokenSecret);
        var authResult = new TwitterAuthResultDto();
        authResult.Data = userInfo;

        return authResult;
    }

    private async Task<TwitterUserAuthInfoDto> SaveAuthUserExtraInfoAsync(string oauthAccessToken,
        string oauthTokenSecret)
    {
        var url = "https://api.twitter.com/2/users/me";
        var timestamp = TimeHelper.GetTimeStampInSeconds().ToString();
        var nonce = Auth10aHelper.GenerateNonce();

        var signature = Auth10aHelper.GenerateSignature(new Uri(url), _options.CustomKey, _options.CustomSecret,
            token: oauthAccessToken, tokenSecret: oauthTokenSecret,
            httpMethod: "GET", timestamp, nonce, verifier: string.Empty, string.Empty,
            out string normalizedUrl, out string normalizedRequestParameters);

        var authorizationHeaderParams =
            "oauth_consumer_key=\"" + _options.CustomKey + "\",oauth_nonce=\"" + nonce +
            "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" + timestamp +
            "\",oauth_token=\"" + oauthAccessToken +
            "\",oauth_version=\"1.0A\",oauth_signature=\"" +
            Auth10aHelper.UrlEncode(signature) + "\"";

        var userInfo = await SaveUserExtraInfoAsync(authorizationHeaderParams, version: "1.0A");
        return new TwitterUserAuthInfoDto
        {
            AccessToken = authorizationHeaderParams,
            UserInfo = userInfo.Data
        };
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

    private async Task<TwitterUserInfoDto> SaveUserExtraInfoAsync(string accessToken, string version = "2.0")
    {
        var authToken = $"{CommonConstant.JwtTokenPrefix} {accessToken}";
        if (version != "2.0")
        {
            authToken = $"OAuth {accessToken}";
            Logger.LogInformation("1.0a token: {token}", accessToken);
        }

        var header = new Dictionary<string, string>
        {
            [CommonConstant.AuthHeader] = authToken
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