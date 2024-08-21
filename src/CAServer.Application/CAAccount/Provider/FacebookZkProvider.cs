using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class FacebookZkProvider : CAServerAppService,  IFacebookZkProvider
{
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ILogger<FacebookZkProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    
    public FacebookZkProvider(IGuardianUserProvider guardianUserProvider,
        ILogger<FacebookZkProvider> logger,
        IHttpClientFactory httpClientFactory,
        IVerifierServerClient verifierServerClient,
        JwtSecurityTokenHandler jwtSecurityTokenHandler)
    {
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _verifierServerClient = verifierServerClient;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
    }
    public async Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        try
        {
            // var facebookUser = await GetFacebookUserInfoAsync(requestDto);
            var facebookUser = ExtractUserInfoFromJwt(requestDto.Jwt);
            var userSaltAndHash = await _guardianUserProvider.GetSaltAndHashAsync(facebookUser.Id, requestDto.Salt, requestDto.PoseidonIdentifierHash);
            if (!userSaltAndHash.Item3)
            {
                await _guardianUserProvider.AddGuardianAsync(facebookUser.Id, userSaltAndHash.Item2, userSaltAndHash.Item1, requestDto.PoseidonIdentifierHash);
            }

            await _guardianUserProvider.AddUserInfoAsync(
                ObjectMapper.Map<FacebookUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>(facebookUser));
            return userSaltAndHash.Item1;
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Verify Facebook Failed");
            throw new UserFriendlyException("Verify Facebook Failed.");
        }
    }

    private FacebookUserInfoDto ExtractUserInfoFromJwt(string jwt)
    {
        if (jwt.IsNullOrWhiteSpace())
        {
            return new FacebookUserInfoDto();
        }
        var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(jwt);
        var sub = jwtToken.Payload["sub"].ToString();
        var givenName = jwtToken.Payload["given_name"].ToString();
        var familyName = jwtToken.Payload["family_name"].ToString();
        var name = jwtToken.Payload["name"].ToString();
        var picture = jwtToken.Payload["picture"].ToString();
        return new FacebookUserInfoDto
        {
            Id = sub,
            FullName = name,
            FirstName = givenName,
            LastName = familyName,
            GuardianType = GuardianIdentifierType.Facebook.ToString(),
            Picture = picture
        };
    }
    
    private async Task<FacebookUserInfoDto> GetFacebookUserInfoAsync(VerifiedZkLoginRequestDto requestDto)
    {
        var verifyFacebookUserInfoDto = await _verifierServerClient.VerifyFacebookAccessTokenAsync(new VerifyTokenRequestDto()
        {
            AccessToken = requestDto.AccessToken,
            ChainId = requestDto.ChainId,
            VerifierId = requestDto.VerifierId,
        });
        if (!verifyFacebookUserInfoDto.Success)
        {
            throw new UserFriendlyException(verifyFacebookUserInfoDto.Message);
        }

        var getUserInfoUrl =
            "https://graph.facebook.com/" + verifyFacebookUserInfoDto.Data.UserId +
            "?fields=id,name,email,picture&access_token=" +
            requestDto.AccessToken;
        var facebookUserResponse = await FacebookRequestAsync(getUserInfoUrl);
        var facebookUserInfo = JsonConvert.DeserializeObject<FacebookUserInfoDto>(facebookUserResponse);
        facebookUserInfo.Picture = facebookUserInfo.PictureDic["data"].Url;
        facebookUserInfo.GuardianType = GuardianIdentifierType.Facebook.ToString();
        return facebookUserInfo;
    }
    
    private async Task<string> FacebookRequestAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        var result = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("FacebookRequest Error {Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (response.IsSuccessStatusCode)
        {
            return result;
        }

        _logger.LogError("{Message}", response.ToString());
        throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
    }
}