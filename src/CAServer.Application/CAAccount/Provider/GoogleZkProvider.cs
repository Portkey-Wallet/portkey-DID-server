using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GoogleZkProvider(
    IGuardianUserProvider guardianUserProvider,
    ILogger<GoogleZkProvider> logger,
    IHttpClientFactory httpClientFactory) : CAServerAppService, IGoogleZkProvider
{
    
    public async Task<string> SaveGuardianUserBeforeZkLoginAsync(VerifiedZkLoginRequestDto requestDto)
    {
        try
        {
            var userInfo = await GetUserInfoFromGoogleAsync(requestDto.AccessToken);
            var hashInfo = await guardianUserProvider.GetSaltAndHashAsync(userInfo.Id);
            if (!hashInfo.Item3)
            {
                await guardianUserProvider.AddGuardianAsync(userInfo.Id, hashInfo.Item2, hashInfo.Item1);
            }
            await guardianUserProvider.AddUserInfoAsync(ObjectMapper.Map<GoogleUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>(userInfo));
            return hashInfo.Item1;
        }
        catch (Exception e)
        {
            logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }
    
    private async Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken)
    {
        var requestUrl = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}";

        var client = httpClientFactory.CreateClient();
        logger.LogInformation("{message}", $"GetUserInfo from google {requestUrl}");
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        var result = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("{Message}", response.ToString());
            throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
        }

        logger.LogInformation("GetUserInfo from google: {userInfo}", result);
        var googleUserInfo = JsonConvert.DeserializeObject<GoogleUserInfoDto>(result);
        if (googleUserInfo == null)
        {
            throw new Exception("Get userInfo from google fail.");
        }

        return googleUserInfo;
    }
}