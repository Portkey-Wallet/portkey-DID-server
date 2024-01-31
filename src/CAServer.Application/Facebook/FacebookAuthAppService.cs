using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Facebook.Dtos;
using CAServer.Signature.Provider;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Facebook;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class FacebookAuthAppService : CAServerAppService, IFacebookAuthAppService
{
    private readonly FacebookOptions _facebookOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FacebookAuthAppService> _logger;
    private readonly ISecretProvider _secretProvider;

    public FacebookAuthAppService(IOptionsSnapshot<FacebookOptions> facebookOptions,
        IHttpClientFactory httpClientFactory, ILogger<FacebookAuthAppService> logger, ISecretProvider secretProvider)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _secretProvider = secretProvider;
        _facebookOptions = facebookOptions.Value;
    }


    public async Task<FacebookAuthResponse> ReceiveAsync(string code, ApplicationType applicationType)
    {
        //var secret = await _secretProvider.GetSecretWithCacheAsync(_facebookOptions.AppId);
        var redirectUrl = applicationType switch
        {
            ApplicationType.Recevie => _facebookOptions.RedirectUrl,
            ApplicationType.UnifyReceive => _facebookOptions.UnifyRedirectUrl,
            _ => _facebookOptions.RedirectUrl
        };

        var url = "https://graph.facebook.com/v19.0/oauth/access_token?client_id=" + _facebookOptions.AppId +
                  "&redirect_uri=" + redirectUrl +
                  "&client_secret=" + _facebookOptions.AppSecret +
                  "&code=" + code;

        var result = await HttpRequestAsync(url);
        var facebookOauthInfo = JsonConvert.DeserializeObject<FacebookOauthResponse>(result);
        if (facebookOauthInfo.AccessToken.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid token.");
        }

        var app_token = _facebookOptions.AppId + "%7C" + _facebookOptions.AppSecret;
        var requestUrl =
            "https://graph.facebook.com/debug_token?access_token=" + app_token + "&input_token=" +
            facebookOauthInfo.AccessToken;

        var verifyResponse = await HttpRequestAsync(requestUrl);
        var facebookVerifyResponse = JsonConvert.DeserializeObject<VerifyFacebookUserInfoResponseDto>(verifyResponse);
        if (!facebookVerifyResponse.Data.IsValid)
        {
            throw new UserFriendlyException("Invalid token.");
        }

        return new FacebookAuthResponse
        {
            UserId = facebookVerifyResponse.Data.UserId,
            AccessToken = facebookOauthInfo.AccessToken,
            ExpiresTime = facebookVerifyResponse.Data.ExpiresAt
        };
    }

    private async Task<string> HttpRequestAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token.");
        }

        if (response.IsSuccessStatusCode)
        {
            return result;
        }

        _logger.LogError("{Message}", response.ToString());
        throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
    }
}