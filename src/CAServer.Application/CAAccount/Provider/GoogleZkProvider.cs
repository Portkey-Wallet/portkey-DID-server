using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GoogleZkProvider : CAServerAppService, IGoogleZkProvider
{
    private const string Domain = "com.portkey.finance://oauthredirect";
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ILogger<GoogleZkProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IGetVerifierServerProvider _verifierServerProvider;
    private readonly IObjectMapper _objectMapper;
    
    public GoogleZkProvider(
        IGuardianUserProvider guardianUserProvider,
        ILogger<GoogleZkProvider> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IVerifierServerClient verifierServerClient,
        IGetVerifierServerProvider verifierServerProvider,
        IObjectMapper objectMapper)
    {
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _verifierServerClient = verifierServerClient;
        _verifierServerProvider = verifierServerProvider;
        _objectMapper = objectMapper;
    }
    
    public async Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        try
        {
            var userInfo = await GetUserInfoFromGoogleAsync(requestDto.AccessToken);
            var hashInfo = await _guardianUserProvider.GetSaltAndHashAsync(userInfo.Id, requestDto.Salt, requestDto.PoseidonIdentifierHash);
            var verifyTokenRequestDto = await PrepareEmailParams(requestDto, userInfo, hashInfo);
            SendNotification(verifyTokenRequestDto, hashInfo);
            if (!hashInfo.Item3)
            {
                await _guardianUserProvider.AddGuardianAsync(userInfo.Id, hashInfo.Item2, hashInfo.Item1, requestDto.PoseidonIdentifierHash);
            }
            await _guardianUserProvider.AddUserInfoAsync(ObjectMapper.Map<GoogleUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>(userInfo));
            return hashInfo.Item1;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<VerifyTokenRequestDto> PrepareEmailParams(VerifiedZkLoginRequestDto requestDto, GoogleUserInfoDto userInfo, Tuple<string, string, bool> hashInfo)
    {
        if (requestDto.VerifierId.IsNullOrEmpty())
        {
            requestDto.VerifierId = await _verifierServerProvider.GetFirstVerifierServerEndPointAsync(requestDto.ChainId);
        }
        var verifyTokenRequestDto = _objectMapper.Map<VerifiedZkLoginRequestDto, VerifyTokenRequestDto>(requestDto);
        var guardianIdentifier = userInfo.Email.IsNullOrEmpty() ? string.Empty : userInfo.Email;
        await _guardianUserProvider.AppendSecondaryEmailInfo(verifyTokenRequestDto, hashInfo.Item1, guardianIdentifier, GuardianIdentifierType.Google);
        return verifyTokenRequestDto;
    }

    private async void SendNotification(VerifyTokenRequestDto verifyTokenRequestDto, Tuple<string, string, bool> hashInfo)
    {
        var response =
            await _verifierServerClient.VerifyGoogleTokenAsync(verifyTokenRequestDto, hashInfo.Item1, hashInfo.Item2);
        if (!response.Success)
        {
            _logger.LogError($"Validate VerifierGoogle Failed :{response.Message}");
        }
    }

    private async Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken)
    {
        var requestUrl = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}";

        var client = _httpClientFactory.CreateClient();
        _logger.LogInformation("{message}", $"GetUserInfo from google {requestUrl}");
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}", response.ToString());
            throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
        }

        var googleUserInfo = JsonConvert.DeserializeObject<GoogleUserInfoDto>(result);
        if (googleUserInfo == null)
        {
            throw new Exception("Get userInfo from google fail.");
        }

        return googleUserInfo;
    }

    public string GetGoogleAuthRedirectUrl()
    {
        var query = _httpContextAccessor?.HttpContext?.Request.Query;
        var queryString = GetQueryStringFromQueryCollection(query);
        var url = Domain + (queryString.StartsWith("?") ? queryString : "?" + queryString);
        return url;
    }
 
    private static string GetQueryStringFromQueryCollection(IQueryCollection queryCollection)
    {
        var queryBuilder = new StringBuilder();
        foreach (var query in queryCollection)
        {
            foreach (var value in query.Value)
            {
                if (queryBuilder.Length > 0)
                {
                    queryBuilder.Append("&");
                }
                queryBuilder.Append(query.Key);
                queryBuilder.Append("=");
                queryBuilder.Append(value);
            }
        }
        return queryBuilder.ToString();
    }

}