using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.AccountValidator;
using CAServer.AppleVerify;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.CAAccount.Enums;
using CAServer.CAAccount.TonWallet;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Options;
using CAServer.Telegram;
using CAServer.TwitterAuth.Dtos;
using CAServer.TwitterAuth.Etos;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
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
    private readonly ICacheProvider _cacheProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IHttpClientService _httpClientService;
    private readonly IDistributedCache<AppleKeys> _distributedCache;
    private readonly ICAAccountProvider _accountProvider;
    private readonly IEnumerable<IVerificationAlgorithmStrategy> _verificationStrategies;
    private readonly ITonWalletProvider _tonWalletProvider;

    private readonly SendVerifierCodeRequestLimitOptions _sendVerifierCodeRequestLimitOption;
    private readonly IdentityUserManager _userManager;
    private readonly IAppleZkProvider _appleZkProvider;

    private const string SendVerifierCodeInterfaceRequestCountCacheKey =
        "SendVerifierCodeInterfaceRequestCountCacheKey";


    public VerifierAppService(IEnumerable<IAccountValidator> accountValidator, IObjectMapper objectMapper,
        ILogger<VerifierAppService> logger,
        IVerifierServerClient verifierServerClient,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientFactory httpClientFactory,
        JwtSecurityTokenHandler jwtSecurityTokenHandler,
        IOptionsSnapshot<SendVerifierCodeRequestLimitOptions> sendVerifierCodeRequestLimitOption,
        ICacheProvider cacheProvider, IContractProvider contractProvider, IHttpClientService httpClientService,
        IDistributedCache<AppleKeys> distributedCache,
        ICAAccountProvider accountProvider,
        IdentityUserManager userManager,
        IEnumerable<IVerificationAlgorithmStrategy> verificationStrategies,
        ITonWalletProvider tonWalletProvider,
        IAppleZkProvider appleZkProvider)
    {
        _accountValidator = accountValidator;
        _objectMapper = objectMapper;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _httpClientFactory = httpClientFactory;
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        _cacheProvider = cacheProvider;
        _contractProvider = contractProvider;
        _httpClientService = httpClientService;
        _verificationStrategies = verificationStrategies;
        _tonWalletProvider = tonWalletProvider;
        _sendVerifierCodeRequestLimitOption = sendVerifierCodeRequestLimitOption.Value;
        _distributedCache = distributedCache;
        _accountProvider = accountProvider;
        _userManager = userManager;
        _appleZkProvider = appleZkProvider;
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
            var guardianIdentifier = userInfo.Email.IsNullOrEmpty() ? userInfo.Id : userInfo.Email;
            await AppendSecondaryEmailInfo(requestDto, hashInfo.Item1, guardianIdentifier, GuardianIdentifierType.Google);
            var response =
                await _verifierServerClient.VerifyGoogleTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);

            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate VerifierGoogle Failed :{response.Message}");
            }

            if (!hashInfo.Item3)
            {
                await AddGuardianAsync(userInfo.Id, hashInfo.Item2, hashInfo.Item1);
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

    private async Task AppendSecondaryEmailInfo(VerifyTokenRequestDto requestDto, string guardianIdentifierHash,
        string guardianIdentifier, GuardianIdentifierType type)
    {
        if (requestDto.CaHash.IsNullOrEmpty())
        {
            //existed guardian, get secondary email by guardian's identifierHash
            requestDto.SecondaryEmail = await GetSecondaryEmailAsync(guardianIdentifierHash);
        }
        else
        {
            //add guardian operation, get secondary email by caHash
            requestDto.SecondaryEmail = await GetSecondaryEmailByCaHash(requestDto.CaHash);
        }
        requestDto.GuardianIdentifier = guardianIdentifier;
        requestDto.Type = type;
    }

    private async Task<string> GetSecondaryEmailByCaHash(string caHash)
    {
        var caHolder = await GetCaHolderByCaHash(caHash);
        if (!caHolder.Success || caHolder.Data == null)
        {
            throw new UserFriendlyException(caHolder.Message);
        }
        return caHolder.Data.SecondaryEmail;
    }

    private async Task<GrainResultDto<CAHolderGrainDto>> GetCaHolderByCaHash(string caHash)
    {
        var userId = await GetUserId(caHash);
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        return caHolder;
    }

    private async Task<Guid> GetUserId(string caHash)
    {
        var user = await _userManager.FindByNameAsync(caHash);
        if (user != null)
        {
            return user.Id;
        }
        throw new UserFriendlyException("the user doesn't exist, caHash:" + caHash);
    }

    private async Task<string> GetSecondaryEmailAsync(string guardianIdentifierHash)
    {
        if (guardianIdentifierHash.IsNullOrEmpty())
        {
            return string.Empty;
        }
        var guardianIndex = await _accountProvider.GetIdentifiersAsync(guardianIdentifierHash);
        return guardianIndex != null ? guardianIndex.SecondaryEmail : string.Empty;
    }

    public async Task<VerificationCodeResponse> VerifyAppleTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var userId = GetAppleUserId(requestDto.AccessToken);
            var hashInfo = await GetSaltAndHashAsync(userId);
            var userExtraInfo = await _appleZkProvider.GetAppleUserExtraInfo(requestDto.AccessToken);
            var guardianIdentifier = userExtraInfo == null || userExtraInfo.IsPrivateEmail ? string.Empty : userExtraInfo?.Email;
            await AppendSecondaryEmailInfo(requestDto, hashInfo.Item1, guardianIdentifier, GuardianIdentifierType.Apple);
            var response =
                await _verifierServerClient.VerifyAppleTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);
            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate VerifierApple Failed :{response.Message}");
            }

            if (!hashInfo.Item3)
            {
                await AddGuardianAsync(userId, hashInfo.Item2, hashInfo.Item1);
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

    public async Task<VerificationCodeResponse> VerifyTwitterTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var userId = await GetTwitterUserIdAsync(requestDto.AccessToken);
            // statistic
            await StatisticTwitterAsync(userId);
            
            var hashInfo = await GetSaltAndHashAsync(userId);
            await AppendSecondaryEmailInfo(requestDto, hashInfo.Item1, userId, GuardianIdentifierType.Twitter);
            var response =
                await _verifierServerClient.VerifyTwitterTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);
            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate twitter failed :{response.Message}");
            }

            // statistic
            await StatisticTwitterAsync(userId);
            if (!hashInfo.Item3)
            {
                await AddGuardianAsync(userId, hashInfo.Item2, hashInfo.Item1);
            }
            await AddUserInfoAsync(
                ObjectMapper.Map<TwitterUserExtraInfo, Dtos.UserExtraInfo>(response.Data.TwitterUserExtraInfo));

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "VerifyTwitterToken error accessToken:{accessToken}, verifierId:{verifierId}, chainId:{chainId}, targetChainId:{targetChainId}, operationType:{operationType}",
                requestDto.AccessToken, requestDto.VerifierId, requestDto.ChainId,
                requestDto.TargetChainId ?? string.Empty, requestDto.OperationType.ToString());

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


    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeInput input)
    {
        return await _verifierServerClient.VerifyRevokeCodeAsync(input);

    }

    private async Task<string> GetTwitterUserIdAsync(string accessToken)
    {
        var header = new Dictionary<string, string>
        {
            [CommonConstant.AuthHeader] = $"{CommonConstant.JwtTokenPrefix} {accessToken}"
        };
        var userInfo = await _httpClientService.GetAsync<TwitterUserInfoDto>(CommonConstant.TwitterUserInfoUrl, header);

        if (userInfo == null)
        {
            throw new UserFriendlyException("Failed to get user info");
        }
        return userInfo.Data.Id;
    }

    private async Task StatisticTwitterAsync(string userId)
    {
        await _distributedEventBus.PublishAsync(new TwitterStatisticEto
        {
            Id = userId,
            UpdateTime = TimeHelper.GetTimeStampInSeconds()
        });
    }

    public async Task<long> CountVerifyCodeInterfaceRequestAsync(string userIpAddress)
    {
        var expire = TimeSpan.FromHours(_sendVerifierCodeRequestLimitOption.ExpireHours);
        var countCacheItem =
            await _cacheProvider.Get(SendVerifierCodeInterfaceRequestCountCacheKey + ":" + userIpAddress);
        if (countCacheItem.HasValue)
        {
            return await _cacheProvider.Increase(SendVerifierCodeInterfaceRequestCountCacheKey + ":" + userIpAddress, 1,
                null);
        }

        return await _cacheProvider.Increase(SendVerifierCodeInterfaceRequestCountCacheKey + ":" + userIpAddress, 1,
            expire);
    }

    public async Task<bool> GuardianExistsAsync(string guardianIdentifier)
    {
        try
        {
            var resultDto = GetGuardian(guardianIdentifier);
            return resultDto.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetGuardian failed");
            throw new UserFriendlyException(e.Message);
        }
    }


    public async Task<GetVerifierServerResponse> GetVerifierServerAsync(string chainId)
    {
        GetVerifierServersOutput result;
        try
        {
            result = await _contractProvider.GetVerifierServersListAsync(chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get verifier server failed");
            throw new UserFriendlyException(CAServerApplicationConsts.ChooseVerifierServerErrorMsg);
        }

        if (null == result || result.VerifierServers.Count == 0)
        {
            _logger.LogError("Get verifier server failed, result is null or empty");
            throw new UserFriendlyException(CAServerApplicationConsts.ChooseVerifierServerErrorMsg);
        }

        var verifierServer = RandomHelper.GetRandomOfList(result.VerifierServers);
        return _objectMapper.Map<VerifierServer, GetVerifierServerResponse>(verifierServer);
    }

    public async Task<VerificationCodeResponse> VerifyTelegramTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var userId = GetTelegramUserId(requestDto.AccessToken);
            _logger.LogDebug("TeleGram userid is {uid}",userId);
            var hashInfo = await GetSaltAndHashAsync(userId);
            await AppendSecondaryEmailInfo(requestDto, hashInfo.Item1, userId, GuardianIdentifierType.Telegram);
            var response =
                await _verifierServerClient.VerifyTelegramTokenAsync(requestDto, hashInfo.Item1, hashInfo.Item2);
            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate VerifierTelegram Failed :{response.Message}");
            }

            if (!hashInfo.Item3)
            {
                await AddGuardianAsync(userId, hashInfo.Item2, hashInfo.Item1);
            }
            await AddUserInfoAsync(
                ObjectMapper.Map<TelegramUserExtraInfo, Dtos.UserExtraInfo>(response.Data.UserExtraInfo));

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "validate telegram error, accessToken={0}, verifierId={1}", requestDto.AccessToken,
                requestDto.VerifierId);
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

    public async Task<VerificationCodeResponse> VerifyFacebookTokenAsync(VerifyTokenRequestDto requestDto)
    {
        try
        {
            var facebookUser = await GetFacebookUserInfoAsync(requestDto);
            var userSaltAndHash = await GetSaltAndHashAsync(facebookUser.Id);
            var guardianIdentifier = facebookUser.Email.IsNullOrEmpty() ? facebookUser.Id : facebookUser.Email;
            await AppendSecondaryEmailInfo(requestDto, userSaltAndHash.Item1, guardianIdentifier, GuardianIdentifierType.Facebook);
            requestDto.SecondaryEmail = facebookUser.Email.IsNullOrEmpty() ? requestDto.SecondaryEmail : facebookUser.Email;
            var response =
                await _verifierServerClient.VerifyFacebookTokenAsync(requestDto, userSaltAndHash.Item1,
                    userSaltAndHash.Item2);
            if (!response.Success)
            {
                throw new UserFriendlyException($"Validate Facebook Failed :{response.Message}");
            }

            if (!userSaltAndHash.Item3)
            {
                await AddGuardianAsync(facebookUser.Id, userSaltAndHash.Item2, userSaltAndHash.Item1);
            }
            await AddUserInfoAsync(
                ObjectMapper.Map<FacebookUserInfoDto, Dtos.UserExtraInfo>(facebookUser));
            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Verify Facebook Failed, {Message}", e.Message);
            throw new UserFriendlyException("Verify Facebook Failed.");
        }
    }

    public async Task<VerificationCodeResponse> VerifyTonWalletAsync(VerifyEdaAlgorithmRequestDto requestDto)
    {
        try
        {
            var guardianIdentifier = requestDto.TonWalletRequest.UserFriendlyAddress;
            var hashInfo = await GetSaltAndHashAsync(guardianIdentifier);
            var verifyTokenRequestDto = new VerifyTokenRequestDto
            {
                AccessToken = requestDto.AccessToken,
                ChainId = requestDto.ChainId,
                TargetChainId = requestDto.TargetChainId, 
                OperationType = requestDto.OperationType,
                OperationDetails = requestDto.OperationDetails,
            };
            await AppendSecondaryEmailInfo(verifyTokenRequestDto, hashInfo.Item1, guardianIdentifier, GuardianIdentifierType.TonWallet);
            var response =
                await _verifierServerClient.VerifyTonWalletTokenAsync(verifyTokenRequestDto, hashInfo.Item1, hashInfo.Item2);
            if (response is not { Success: true } || response.Data is not { Result: true })
            {
                _logger.LogWarning("send notification email error verifyTokenRequestDto:{0}", JsonConvert.SerializeObject(verifyTokenRequestDto));
            }
            if (!hashInfo.Item3)
            {
                await AddGuardianAsync(guardianIdentifier, hashInfo.Item2, hashInfo.Item1);
            }
            
            var verificationStrategy = _verificationStrategies
                .FirstOrDefault(v => v.VerifierType.Equals(VerifierType.TonWallet));
            if (verificationStrategy == null)
            {
                throw new UserFriendlyException("verification Strategy not exist");
            }

            var message = _tonWalletProvider.GetTonWalletMessage(requestDto.TonWalletRequest.Request);
            return new VerificationCodeResponse
            {
                Extra = verificationStrategy.ExtraHandler(hashInfo.Item2, message),
                GuardianIdentifierHash = hashInfo.Item1
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<FacebookUserInfoDto> GetFacebookUserInfoAsync(VerifyTokenRequestDto requestDto)
    {
        var verifyFacebookUserInfoDto = await _verifierServerClient.VerifyFacebookAccessTokenAsync(requestDto);

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
            _logger.LogError("{Message}", response.ToString());
            throw new Exception("Invalid token");
        }

        if (response.IsSuccessStatusCode)
        {
            return result;
        }

        _logger.LogError("{Message}", response.ToString());
        throw new Exception($"StatusCode: {response.StatusCode.ToString()}, Content: {result}");
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

        var userExtraInfoEto = _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto);
        _logger.LogDebug("Publish user extra info to mq: {data}", JsonConvert.SerializeObject(userExtraInfoEto));
        await _distributedEventBus.PublishAsync(userExtraInfoEto);
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
            salt = GetSalt().ToHex();
            identifierHash = GetHash(Encoding.UTF8.GetBytes(requestInput.GuardianIdentifier),
                ByteArrayHelper.HexStringToByteArray(salt)).ToHex();
        }

        requestInput.Salt = salt;
        requestInput.GuardianIdentifierHash = identifierHash;

        return guardianGrainResult;
    }

    /// <summary>
    /// </summary>
    /// <param name="guardianIdentifier">guardianIdentifier</param>
    /// <returns>
    /// item1:identifierHash
    /// item2:salt
    /// item3:guardianGrainResult
    /// </returns>
    private async Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier)
    {
        var guardianGrainResult = GetGuardian(guardianIdentifier);

        _logger.LogInformation("GetGuardian info, guardianIdentifier: {result}",
            JsonConvert.SerializeObject(guardianGrainResult));

        if (guardianGrainResult.Success)
        {
            return Tuple.Create(guardianGrainResult.Data.IdentifierHash, guardianGrainResult.Data.Salt, true);
        }

        var salt = GetSalt();
        var identifierHash = GetHash(Encoding.UTF8.GetBytes(guardianIdentifier), salt);

        return Tuple.Create(identifierHash.ToHex(), salt.ToHex(), false);
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

    private byte[] GetSalt() => Guid.NewGuid().ToByteArray();

    private Hash GetHash(byte[] identifier, byte[] salt)
    {
        const int maxIdentifierLength = 256;
        const int maxSaltLength = 16;

        if (identifier.Length > maxIdentifierLength)
        {
            throw new Exception("Identifier is too long");
        }

        if (salt.Length != maxSaltLength)
        {
            throw new Exception($"Salt has to be {maxSaltLength} bytes.");
        }

        var hash = HashHelper.ComputeFrom(identifier);
        return HashHelper.ComputeFrom(hash.Concat(salt).ToArray());
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

    private string GetTelegramUserId(string identityToken)
    {
        try
        {
            var jwtToken = _jwtSecurityTokenHandler.ReadJwtToken(identityToken);
            var claims = jwtToken.Payload.Claims;
            var idClaims = claims.FirstOrDefault(c => c.Type.Equals(TelegramTokenClaimNames.UserId));
            var userId = idClaims?.Value;
            if (userId.IsNullOrWhiteSpace())
            {
                throw new Exception("userId is empty");
            }

            return userId;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "invalid jwt token, {token}", identityToken);
            throw new Exception("Invalid token");
        }
    }
    
    public async Task<CAHolderResultDto> GetHolderInfoByCaHashAsync(string caHash)
    {
        var result = await GetCaHolderByCaHash(caHash);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }
        return ObjectMapper.Map<CAHolderGrainDto, CAHolderResultDto>(result.Data);
    }
}
