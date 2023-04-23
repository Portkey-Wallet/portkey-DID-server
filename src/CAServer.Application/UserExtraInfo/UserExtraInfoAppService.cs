using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Auditing;
using System.Net.Http;
using CAServer.AppleAuth;
using CAServer.AppleAuth.Provider;
using CAServer.AppleVerify;
using CAServer.CAAccount.Dtos;
using CAServer.UserExtraInfo.Dtos;
using CAServer.Common;
using CAServer.Grains;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.UserExtraInfo;

[RemoteService(false)]
[DisableAuditing]
public class UserExtraInfoAppService : CAServerAppService, IUserExtraInfoAppService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppleAuthOptions _appleAuthOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IHttpClientService _httpClientService;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly IAppleUserProvider _appleUserProvider;

    public UserExtraInfoAppService(IHttpClientFactory httpClientFactory,
        IOptions<AppleAuthOptions> appleAuthVerifyOption,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientService httpClientService,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IAppleUserProvider appleUserProvider)
    {
        _httpClientFactory = httpClientFactory;
        _appleAuthOptions = appleAuthVerifyOption.Value;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpClientService = httpClientService;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _appleUserProvider = appleUserProvider;
    }


    public async Task<AddAppleUserExtraInfoResultDto> AddAppleUserExtraInfoAsync(AddAppleUserExtraInfoDto extraInfoDto)
    {
        var securityToken = await ValidateTokenAsync(extraInfoDto.IdentityToken);
        var jwtPayload = ((JwtSecurityToken)securityToken).Payload;
        var userInfo = new Verifier.Dtos.UserExtraInfo
        {
            Id = jwtPayload.Sub,
            FirstName = extraInfoDto.UserInfo.Name.FirstName,
            LastName = extraInfoDto.UserInfo.Name.LastName,
            Email = extraInfoDto.UserInfo.Email,
            GuardianType = GuardianIdentifierType.Apple.ToString(),
            AuthTime = DateTime.UtcNow
        };

        await _appleUserProvider.SetUserExtraInfoAsync(new AppleUserExtraInfo
        {
            UserId = userInfo.Id,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
        });

        if (jwtPayload.ContainsKey("email_verified"))
        {
            userInfo.VerifiedEmail = Convert.ToBoolean(jwtPayload["email_verified"]);
        }

        if (jwtPayload.ContainsKey("is_private_email"))
        {
            userInfo.IsPrivateEmail = Convert.ToBoolean(jwtPayload["is_private_email"]);
        }

        await AddUserInfoAsync(userInfo);

        return new AddAppleUserExtraInfoResultDto { UserId = userInfo.Id };
    }

    public async Task<UserExtraInfoResultDto> GetUserExtraInfoAsync(string id)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", id);

        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);
        var resultDto = await userExtraInfoGrain.GetAsync();
        if (resultDto.Success)
        {
            await SetNameAsync(resultDto.Data);
            return ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoResultDto>(resultDto.Data);
        }
        else
        {
            var userInfo = await _appleUserProvider.GetUserExtraInfoAsync(id);
            if (userInfo == null)
            {
                throw new UserFriendlyException(resultDto.Message);
            }

            var userExtraInfo = new Verifier.Dtos.UserExtraInfo
            {
                Id = userInfo.UserId,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                //Email = UserInfo.Email,
                GuardianType = GuardianIdentifierType.Apple.ToString(),
                AuthTime = DateTime.UtcNow
            };

            await AddUserInfoAsync(userExtraInfo);
            return ObjectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoResultDto>(userExtraInfo);
        }
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

    private async Task<SecurityToken> ValidateTokenAsync(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header["kid"].ToString();

            var appleKeys = await GetAppleKeyFormAppleAsync();
            var jwk = new JsonWebKey(
                JsonConvert.SerializeObject(appleKeys.Keys.FirstOrDefault(t => t.Kid == kid)));

            var validateParameter = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudiences = _appleAuthOptions.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwk
            };

            _jwtSecurityTokenHandler.ValidateToken(identityToken, validateParameter,
                out SecurityToken validatedToken);

            return validatedToken;
        }
        catch (SecurityTokenExpiredException e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("The token has expired.");
        }
        catch (SecurityTokenException e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("Valid token fail.");
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException("Invalid token.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<AppleKeys> GetAppleKeyFormAppleAsync()
    {
        var appleKeyUrl = "https://appleid.apple.com/auth/keys";
        return await _httpClientService.GetAsync<AppleKeys>(appleKeyUrl);
    }

    private async Task SetNameAsync(UserExtraInfoGrainDto userExtraInfo)
    {
        var userInfo = await _appleUserProvider.GetUserExtraInfoAsync(userExtraInfo.Id);
        if (userInfo == null) return;

        userExtraInfo.FirstName = userInfo.FirstName;
        userExtraInfo.LastName = userInfo.LastName;
    }
}