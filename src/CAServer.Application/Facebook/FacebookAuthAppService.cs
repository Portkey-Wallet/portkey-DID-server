using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
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
    private const string UrlCode = "%7C";

    public FacebookAuthAppService(IOptionsSnapshot<FacebookOptions> facebookOptions,
        IHttpClientFactory httpClientFactory, ILogger<FacebookAuthAppService> logger, ISecretProvider secretProvider)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _secretProvider = secretProvider;
        _facebookOptions = facebookOptions.Value;
    }


    public async Task<FacebookAuthResponseDto> ReceiveAsync(string code, ApplicationType applicationType)
    {
        if (string.IsNullOrEmpty(code))
        {
            return new FacebookAuthResponseDto
            {
                Code = AuthErrorMap.FacebookCancelCode,
                Message = AuthErrorMap.ErrorMapInfo[AuthErrorMap.FacebookCancelCode]
            };
        }

        try
        {
            var secret = await _secretProvider.GetSecretWithCacheAsync(_facebookOptions.AppId);
            var redirectUrl = applicationType switch
            {
                ApplicationType.Receive => _facebookOptions.RedirectUrl,
                ApplicationType.UnifyReceive => _facebookOptions.UnifyRedirectUrl,
                _ => _facebookOptions.RedirectUrl
            };
            var url = "https://graph.facebook.com/v19.0/oauth/access_token?client_id=" + _facebookOptions.AppId +
                      "&redirect_uri=" + redirectUrl +
                      "&client_secret=" + secret +
                      "&code=" + code;

            var result = await HttpRequestAsync(url);
            var facebookOauthInfo = JsonConvert.DeserializeObject<FacebookOauthResponse>(result);
            if (facebookOauthInfo.AccessToken.IsNullOrEmpty())
            {
                throw new UserFriendlyException("Invalid token.");
            }

            var app_token = _facebookOptions.AppId + UrlCode + secret;
            var requestUrl =
                "https://graph.facebook.com/debug_token?access_token=" + app_token + "&input_token=" +
                facebookOauthInfo.AccessToken;

            var verifyResponse = await HttpRequestAsync(requestUrl);
            var facebookVerifyResponse =
                JsonConvert.DeserializeObject<VerifyFacebookUserInfoResponseDto>(verifyResponse);
            if (!facebookVerifyResponse.Data.IsValid)
            {
                throw new UserFriendlyException("Invalid token.");
            }

            return new FacebookAuthResponseDto
            {
                Data = new FacebookAuthResponse
                {
                    UserId = facebookVerifyResponse.Data.UserId,
                    AccessToken = facebookOauthInfo.AccessToken,
                    ExpiresTime = facebookVerifyResponse.Data.ExpiresAt
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Facebook auth failed : {Message}", e.Message);
            return new FacebookAuthResponseDto
            {
                Code = AuthErrorMap.DefaultCode,
                Message = AuthErrorMap.ErrorMapInfo[AuthErrorMap.DefaultCode]
            };
        }
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