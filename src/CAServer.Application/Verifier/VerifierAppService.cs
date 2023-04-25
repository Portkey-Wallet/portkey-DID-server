using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using CAServer.Guardian;
using CAServer.AccountValidator;
using CAServer.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Options;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.Verifier;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class VerifierAppService : CAServerAppService, IVerifierAppService
{
    private readonly IEnumerable<IAccountValidator> _accountValidator;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<VerifierAppService> _logger;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly GoogleRecaptchaOptions _googleRecaptchaOption;


   

    public VerifierAppService(IEnumerable<IAccountValidator> accountValidator, IObjectMapper objectMapper,
        ILogger<VerifierAppService> logger,
        IVerifierServerClient verifierServerClient,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientFactory httpClientFactory,
        JwtSecurityTokenHandler jwtSecurityTokenHandler, IOptions<GoogleRecaptchaOptions> googleRecaptchaOption)
    {
        _accountValidator = accountValidator;
        _objectMapper = objectMapper;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpClientFactory = httpClientFactory;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _googleRecaptchaOption = googleRecaptchaOption.Value;
    }

    public async Task<VerifierServerResponse> SendVerificationRequestAsync(SendVerificationRequestInput input)
    {
        //validate 
        var startTime = DateTime.UtcNow.ToUniversalTime();
        try
        {
            ValidateAccount(input);
            var verifierSessionId = Guid.NewGuid();
            input.VerifierSessionId = verifierSessionId;
            var dto = _objectMapper.Map<SendVerificationRequestInput, VerifierCodeRequestDto>(input);
            var result = await _verifierServerClient.SendVerificationRequestAsync(dto);
            if (result.Success)
            {
                return new VerifierServerResponse
                {
                    VerifierSessionId = verifierSessionId
                };
            }

            _logger.LogError("Send VerifierCode Failed : {message}", result.Message);
            throw new UserFriendlyException(result.Message);
        }
        catch (Exception e)
        {
            var endTime = DateTime.UtcNow.ToUniversalTime();
            var costTime = (endTime - startTime).TotalMilliseconds;
            _logger.LogDebug("TotalCount Time is {time}", (long)costTime);
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    public async Task<VerificationCodeResponse> VerifyCodeAsync(VerificationSignatureRequestDto signatureRequestDto)
    {
        try
        {
            var request =
                _objectMapper.Map<VerificationSignatureRequestDto, VierifierCodeRequestInput>(signatureRequestDto);

            var guardianGrainResult = GetSaltAndHash(request);

            var response = await _verifierServerClient.VerifyCodeAsync(request);
            if (!response.Success)
            {
                throw new UserFriendlyException("Validate VerifierCode Failed :" + response.Message);
            }

            if (!guardianGrainResult.Success)
            {
                await AddGuardianAsync(signatureRequestDto.GuardianIdentifier, request.Salt,
                    request.GuardianIdentifierHash);
            }

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    public async Task<VerificationCodeResponse> VerifyGoogleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var userInfo = await GetUserInfoFromGoogleAsync(requestDto.AccessToken);
            var hashInfo = await GetSaltAndHashAsync(userInfo.Id);
            var response =
                await _verifierServerClient.VerifyGoogleTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);

            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate VerifierGoogle Failed :{response.Message}");
            }

            await AddUserInfoAsync(
                ObjectMapper.Map<GoogleUserExtraInfo, Dtos.UserExtraInfo>(response.Data.GoogleUserExtraInfo));

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);

            if (ThirdPartyMessage.MessageDictionary.ContainsKey(e.Message))
            {
                throw new UserFriendlyException(e.Message, ThirdPartyMessage.MessageDictionary[e.Message]);
            }

            if (e.Message.ToLower().Contains("timeout"))
            {
                throw new UserFriendlyException("Request time out",
                    ThirdPartyMessage.MessageDictionary["Request time out"]);
            }

            throw new UserFriendlyException(e.Message);
        }
    }

    public async Task<VerificationCodeResponse> VerifyAppleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var userId = GetAppleUserId(requestDto.AccessToken);
            var hashInfo = await GetSaltAndHashAsync(userId);
            var response =
                await _verifierServerClient.VerifyAppleTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);
            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate VerifierApple Failed :{response.Message}");
            }

            await AddUserInfoAsync(
                ObjectMapper.Map<AppleUserExtraInfo, Dtos.UserExtraInfo>(response.Data.AppleUserExtraInfo));

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            if (ThirdPartyMessage.MessageDictionary.ContainsKey(e.Message))
            {
                throw new UserFriendlyException(e.Message, ThirdPartyMessage.MessageDictionary[e.Message]);
            }

            if (e.Message.ToLower().Contains("timeout"))
            {
                throw new UserFriendlyException("Request time out",
                    ThirdPartyMessage.MessageDictionary["Request time out"]);
            }

            throw new UserFriendlyException(e.Message);
        }
    }

    public async Task<bool> VerifyGoogleRecaptchaTokenAsync(string recaptchaToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", _googleRecaptchaOption.Secret),
            new KeyValuePair<string, string>("response", recaptchaToken)
        });
        _logger.LogDebug("VerifyGoogleRecaptchaToken content is {content}",content.ToString());
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(_googleRecaptchaOption.VerifyUrl, content);
        _logger.LogDebug("response is {response}",response.ToString());
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(" VerifyGoogleRecaptchaToken responseContent is {responseContent}",responseContent);
        return responseContent.Contains("\"success\": true");
    
    }

    private async Task AddUserInfoAsync(Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);
        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAsync(
            _objectMapper.Map<Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;

        Logger.LogInformation("Add or update user extra info success, Publish to MQ: {data}",
            JsonConvert.SerializeObject(userExtraInfo));

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto));
    }

    private GrainResultDto<GuardianGrainDto> GetSaltAndHash(VierifierCodeRequestInput requestInput)
    {
        var guardianGrainResult = GetGuardian(requestInput.GuardianIdentifier);
        string salt;
        string identifierHash;
        if (guardianGrainResult.Success)
        {
            salt = guardianGrainResult.Data.Salt;
            identifierHash = guardianGrainResult.Data.IdentifierHash;
        }
        else
        {
            salt = GetSalt();
            identifierHash = GetHash(requestInput.GuardianIdentifier, requestInput.Salt);
        }

        requestInput.Salt = salt;
        requestInput.GuardianIdentifierHash = identifierHash;

        return guardianGrainResult;
    }

    private async Task<Tuple<string, string>> GetSaltAndHashAsync(string guardianIdentifier)
    {
        var guardianGrainResult = GetGuardian(guardianIdentifier);

        _logger.LogInformation("GetGuardian info, guardianIdentifier: {result}",
            JsonConvert.SerializeObject(guardianGrainResult));
        
        if (guardianGrainResult.Success)
        {
            return Tuple.Create(guardianGrainResult.Data.IdentifierHash, guardianGrainResult.Data.Salt);
        }

        var salt = GetSalt();
        var identifierHash = GetHash(guardianIdentifier, salt);
        await AddGuardianAsync(guardianIdentifier, salt, identifierHash);

        return Tuple.Create(identifierHash, salt);
    }

    private GrainResultDto<GuardianGrainDto> GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        return guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
    }

    private async Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.AddGuardianAsync(guardianIdentifier, salt, identifierHash);

        _logger.LogInformation("AddGuardianAsync result: {result}", JsonConvert.SerializeObject(guardianGrainDto));
        if (guardianGrainDto.Success)
        {
            _logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
                JsonConvert.SerializeObject(guardianGrainDto.Data));

            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data));
        }
    }

    private void ValidateAccount(SendVerificationRequestInput input)
    {
        var validator = _accountValidator.FirstOrDefault(v => v.Type == input.Type);
        if (validator == null)
        {
            throw new UserFriendlyException("InvalidInput type.");
        }

        if (!validator.Validate(input.GuardianIdentifier))
        {
            throw new UserFriendlyException("InvalidInput GuardianIdentifier");
        }
    }

    private string GetSalt() => Guid.NewGuid().ToString("N");

    private string GetHash(string input, string salt)
    {
        var hash = HashHelper.ComputeFrom(input).ToHex();
        return HashHelper.ComputeFrom(salt + hash).ToHex();
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

        _logger.LogInformation("GetUserInfo from google: {userInfo}", result);
        var googleUserInfo = JsonConvert.DeserializeObject<GoogleUserInfoDto>(result);
        if (googleUserInfo == null)
        {
            throw new Exception("Get userInfo from google fail.");
        }

        return googleUserInfo;
    }

    private string GetAppleUserId(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            return jwtToken.Payload.Sub;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new Exception("Invalid token");
        }
    }
}