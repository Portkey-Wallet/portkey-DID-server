using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Grains;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Signature.Provider;
using CAServer.TwitterAuth.Dtos;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public TwitterAuthAppService(IHttpClientService httpClientService, IOptionsSnapshot<TwitterAuthOptions> options,
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IHttpContextAccessor httpContextAccessor, ISecretProvider secretProvider, IHttpClientFactory httpClientFactory)
    {
        _httpClientService = httpClientService;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpContextAccessor = httpContextAccessor;
        _secretProvider = secretProvider;
        _httpClientFactory = httpClientFactory;
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
            return authResult;
        }
    }

    private async Task<TwitterUserAuthInfoDto> ValidAuthCodeAsync(TwitterAuthDto twitterAuthDto)
    {
        var clientSecret = await _secretProvider.GetSecretWithCacheAsync(_options.ClientId);
        Logger.LogInformation("receive twitter callback, data: {data}",
            JsonConvert.SerializeObject(twitterAuthDto));

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
            ["Authorization"] = basicAuth
        };

        var response = await _httpClientService.PostAsync<TwitterTokenDto>(_options.TwitterTokenUrl,
            RequestMediaType.Form, requestParam, header);

        Logger.LogInformation("send code to twitter success, response:{response}",
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
        var url = "https://api.twitter.com/2/users/me";
        var header = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {accessToken}"
        };
        var userInfo = await GetAsync<TwitterUserInfoDto>(url, header);

        if (userInfo == null)
        {
            throw new Exception("Failed to get user info");
        }

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
        string errorCode = "50000";
        if (exception is UserFriendlyException friendlyException)
        {
            errorCode = friendlyException.Code == "429" ? "40003" : friendlyException.Code;
        }

        var message = AuthErrorMap.GetMessage(errorCode);
        return (errorCode, message);
    }

    private async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient();
        foreach (var keyValuePair in headers)
        {
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Logger.LogError(
                "Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return JsonConvert.DeserializeObject<T>(content);
    }
}