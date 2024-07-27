using System;
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
public class FacebookZkProvider(IGuardianUserProvider guardianUserProvider,
    ILogger<FacebookZkProvider> logger,
    IHttpClientFactory httpClientFactory,
    IVerifierServerClient verifierServerClient) : CAServerAppService,  IFacebookZkProvider
{
    
    public async Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        try
        {
            var facebookUser = await GetFacebookUserInfoAsync(requestDto);
            var userSaltAndHash = await guardianUserProvider.GetSaltAndHashAsync(facebookUser.Id);
            if (!userSaltAndHash.Item3)
            {
                await guardianUserProvider.AddGuardianAsync(facebookUser.Id, userSaltAndHash.Item2, userSaltAndHash.Item1);
            }

            await guardianUserProvider.AddUserInfoAsync(
                ObjectMapper.Map<FacebookUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>(facebookUser));
            return userSaltAndHash.Item1;
        }
        catch (Exception e)
        {
            logger.LogError("Verify Facebook Failed, {Message}", e.Message);
            throw new UserFriendlyException("Verify Facebook Failed.");
        }
    }
    
    private async Task<FacebookUserInfoDto> GetFacebookUserInfoAsync(VerifiedZkLoginRequestDto requestDto)
    {
        var verifyFacebookUserInfoDto = await verifierServerClient.VerifyFacebookAccessTokenAsync(new VerifyTokenRequestDto()
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
        var client = httpClientFactory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (response.IsSuccessStatusCode)
        {
            return result;
        }

        logger.LogError("{Message}", response.ToString());
        throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
    }
}