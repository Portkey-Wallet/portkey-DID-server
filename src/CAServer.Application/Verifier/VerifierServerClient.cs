using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Accelerate;
using CAServer.Common;
using CAServer.Dtos;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.Security.Dtos;
using CAServer.Settings;
using CAServer.Tokens;
using CAServer.Tokens.Cache;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using CAServer.Verifier.Dtos;
using CAVerifierServer.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.Verifier;

public class VerifierServerClient : IDisposable, IVerifierServerClient, ISingletonDependency
{
    private readonly IHttpService _httpService;
    private readonly IGetVerifierServerProvider _getVerifierServerProvider;
    private readonly ILogger<VerifierServerClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccelerateManagerProvider _accelerateManagerProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly IContractProvider _contractProvider;
    private readonly ITokenProvider _tokenProvider;
    private readonly ITokenCacheProvider _tokenCacheProvider;
    private readonly IObjectMapper _objectMapper;

    public VerifierServerClient(IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions,
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<VerifierServerClient> logger,
        IHttpClientFactory httpClientFactory, IAccelerateManagerProvider accelerateManagerProvider,
        IOptions<ChainOptions> chainOptions, IIpInfoAppService ipInfoAppService,
        IContractProvider contractProvider, ITokenProvider tokenProvider,
        ITokenCacheProvider tokenCacheProvider, IObjectMapper objectMapper)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, httpClientFactory, true);
        _httpClientFactory = httpClientFactory;
        _accelerateManagerProvider = accelerateManagerProvider;
        _ipInfoAppService = ipInfoAppService;
        _chainOptions = chainOptions.Value;
        _contractProvider = contractProvider;
        _tokenProvider = tokenProvider;
        _tokenCacheProvider = tokenCacheProvider;
        _objectMapper = objectMapper;
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        _disposed = true;
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public async Task<ResponseResultDto<VerifierServerResponse>> SendVerificationRequestAsync(
        VerifierCodeRequestDto dto)
    {
        var endPoint = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(dto.VerifierId, dto.ChainId);
        _logger.LogInformation("EndPiont is {endPiont} :", endPoint);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{verifierId}", dto.VerifierId);
            return new ResponseResultDto<VerifierServerResponse>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }

        var operationDetails =
            _accelerateManagerProvider.GenerateOperationDetails(dto.OperationType, dto.OperationDetails);
        var showOperationDetails = new ShowOperationDetailsDto
        {
            OperationType = GetOperationDecs(dto.OperationType),
            Token = GetDetailDesc(dto.OperationDetails, "symbol"),
            Amount = GetDetailDesc(dto.OperationDetails, "amount"),
            Chain = GetChainDetailDesc(dto.TargetChainId ?? dto.ChainId),
            GuardianType = dto.Type,
            GuardianAccount = dto.GuardianIdentifier,
            Time = DateTime.UtcNow + " UTC",
            IP = await GetIpDetailDesc(),
            ToAddress = GetDetailDesc(dto.OperationDetails, "toAddress"),
            SingleLimit = GetDetailDesc(dto.OperationDetails, "singleLimit"),
            DailyLimit = GetDetailDesc(dto.OperationDetails, "dailyLimit")
        };

        var showOperationDetailsJson = JsonConvert.SerializeObject(showOperationDetails);

        var url = endPoint + "/api/app/account/sendVerificationRequest";
        var parameters = new Dictionary<string, string>
        {
            { "type", dto.Type },
            { "guardianIdentifier", dto.GuardianIdentifier },
            { "verifierSessionId", dto.VerifierSessionId.ToString() },
            { "operationDetails", operationDetails },
            { "showOperationDetails", showOperationDetailsJson }
        };
        return await _httpService.PostResponseAsync<ResponseResultDto<VerifierServerResponse>>(url, parameters);
    }

    public async Task<ResponseResultDto<VerifierServerResponse>> SendSecondaryEmailVerificationRequestAsync(string secondaryEmail, string verifierSessionId)
    {
        var chainInfo = GetMainChain();
        var endPoint = await _getVerifierServerProvider.GetFirstVerifierServerEndPointAsync(chainInfo.ChainId);
        _logger.LogInformation("EndPiont is {endPiont} :", endPoint);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{0}", chainInfo.ChainId);
            return new ResponseResultDto<VerifierServerResponse>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }
        
        var url = endPoint + "/api/app/account/send/secondary/email/verify";
        var parameters = new Dictionary<string, string>
        {
            { "secondaryEmail", secondaryEmail },
            { "verifierSessionId", verifierSessionId }
        };
        ResponseResultDto<VerifierServerResponse> response = null;
        try
        {
            _logger.LogInformation("_httpService.PostResponseAsync url:{0} parameters:{1}", url, JsonConvert.SerializeObject(parameters));
            response = await _httpService.PostResponseAsync<ResponseResultDto<VerifierServerResponse>>(url, parameters);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "_httpService.PostResponseAsync error");
        }
        if (response == null || !response.Success || response.Data == null)
        {
            return response;
        }
        response.Data.VerifierServerEndpoint = endPoint;
        return response;
    }

    public async Task<bool> SendNotificationAfterApprovalAsync(string email, string chainId, OperationType operationType,
        DateTime dateTime, ManagerApprovedDto managerApprovedDto = null)
    {
        var endPoint = await _getVerifierServerProvider.GetFirstVerifierServerEndPointAsync(chainId);
        _logger.LogInformation("EndPiont is {endPiont} :", endPoint);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{0}", chainId);
            return false;
        }
        var showOperationDetails = managerApprovedDto == null ? new ShowOperationDetailsDto()
            {
                OperationType = GetOperationDecs(operationType),
                Chain = GetChainDetailDesc(chainId),
                Time = dateTime + " UTC",
            }
            : new ShowOperationDetailsDto
            {
                OperationType = GetOperationDecs(operationType),
                Token = managerApprovedDto.Symbol,
                Amount = managerApprovedDto.Amount.ToString(),
                Chain = managerApprovedDto.ChainId,
                Time = dateTime + " UTC"
            };
        var showOperationDetailsJson = JsonConvert.SerializeObject(showOperationDetails);

        var url = endPoint + "/api/app/account/sendNotification";
        var parameters = new Dictionary<string, string>
        {
            { "template", EmailTemplate.AfterApproval.ToString() },
            { "email", email },
            { "showOperationDetails", showOperationDetailsJson }
        };
        var response = await _httpService.PostResponseAsync<ResponseResultDto<VerifierServerResponse>>(url, parameters);
        return response is { Success: true };
    }

    private ChainInfo GetMainChain()
    {
        foreach (var chainOptionsChainInfo in 
                 _chainOptions.ChainInfos.Where(chainOptionsChainInfo => chainOptionsChainInfo.Value.IsMainChain))
        {
            return chainOptionsChainInfo.Value;
        }

        throw new UserFriendlyException("There's no main chain");
    }

    public async Task<ResponseResultDto<VerificationCodeResponse>> VerifyCodeAsync(VierifierCodeRequestInput input)
    {
        var endPoint =
            await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{VerifierId}", input.VerifierId);
            return new ResponseResultDto<VerificationCodeResponse>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }

        var operationDetails =
            _accelerateManagerProvider.GenerateOperationDetails(input.OperationType, input.OperationDetails);
        var type = Convert.ToInt32(input.OperationType).ToString();
        var url = endPoint + "/api/app/account/verifyCode";
        var parameters = new Dictionary<string, string>
        {
            { "verifierSessionId", input.VerifierSessionId },
            { "code", input.VerificationCode },
            { "guardianIdentifier", input.GuardianIdentifier },
            { "guardianIdentifierHash", input.GuardianIdentifierHash },
            { "salt", input.Salt },
            { "operationType", type },
            {
                "chainId", string.IsNullOrWhiteSpace(input.TargetChainId)
                    ? ChainHelper.ConvertBase58ToChainId(input.ChainId).ToString()
                    : ChainHelper.ConvertBase58ToChainId(input.TargetChainId).ToString()
            },
            { "operationDetails", operationDetails }
        };


        return await _httpService.PostResponseAsync<ResponseResultDto<VerificationCodeResponse>>(url, parameters);
    }
    
    public async Task<ResponseResultDto<bool>> VerifySecondaryEmailCodeAsync(string verifierSessionId, string verificationCode,
        string secondaryEmail, string verifierEndpoint)
    {
        if (null == verifierEndpoint)
        {
            return new ResponseResultDto<bool>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }

        var url = verifierEndpoint + "/api/app/account/secondaryEmail/verifyCode";
        var parameters = new Dictionary<string, string>
        {
            { "secondaryEmail", secondaryEmail },
            { "verifierSessionId", verifierSessionId },
            { "code", verificationCode }
        };
        return await _httpService.PostResponseAsync<ResponseResultDto<bool>>(url, parameters);
    }

    public async Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyGoogleToken";
        return await GetResultAsync<VerifyGoogleTokenDto>(input, requestUri, identifierHash, salt);
    }

    public async Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyAppleToken";
        return await GetResultAsync<VerifyAppleTokenDto>(input, requestUri, identifierHash, salt);
    }

    public async Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(
        VerifyTokenRequestDto input, string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyTelegramToken";
        return await GetResultAsync<VerifyTokenDto<TelegramUserExtraInfo>>(input, requestUri, identifierHash, salt);
    }
    
    public async Task<ResponseResultDto<EmailNotificationDto>> VerifyTonWalletTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyTonWalletToken";
        var endpoint = await _getVerifierServerProvider.GetFirstVerifierServerEndPointAsync(input.ChainId);
        return await GetResultAsync<EmailNotificationDto>(input, requestUri, identifierHash, salt, endpoint);
    }

    public async Task<ResponseResultDto<VerificationCodeResponse>> VerifyFacebookTokenAsync(
        VerifyTokenRequestDto requestDto, string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyFacebookToken";
        return await GetResultAsync<VerificationCodeResponse>(requestDto, requestUri, identifierHash, salt);
    }

    public async Task<ResponseResultDto<VerifyFacebookUserInfoDto>> VerifyFacebookAccessTokenAsync(
        VerifyTokenRequestDto input)
    {
        var endPoint =
            await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{VerifierId}", input.VerifierId);
            return new ResponseResultDto<VerifyFacebookUserInfoDto>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }

        var url = endPoint + "/api/app/account/verifyFacebookAccessTokenAndGetUserId";
        var parameters = new Dictionary<string, string>
        {
            { "accessToken", input.AccessToken }
        };

        return await _httpService.PostResponseAsync<ResponseResultDto<VerifyFacebookUserInfoDto>>(url, parameters);
    }

    public async Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyTwitterTokenAsync(VerifyTokenRequestDto input,
        string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyTwitterToken";
        return await GetResultAsync<VerifyTwitterTokenDto>(input, requestUri, identifierHash, salt);
    }


    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeInput input)
    {
        var endPoint =
            await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
        _logger.LogInformation("EndPiont is {endPiont} :", endPoint);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{verifierId}", input.VerifierId);
            return false;
        }

        var url = endPoint + "/api/app/account/verifyRevokeCode";
        var parameters = new Dictionary<string, string>
        {
            { "guardianIdentifier", input.GuardianIdentifier },
            { "verifyCode", input.VerifyCode },
            { "verifierSessionId", input.VerifierSessionId.ToString() },
            { "type", input.Type },
        };
        var response = await _httpService.PostResponseAsync<VerifyRevokeCodeResponse>(url, parameters);
        return response.Success;
    }

    private async Task<ResponseResultDto<T>> GetResultAsync<T>(VerifyTokenRequestDto input,
        string requestUri, string identifierHash, string salt, string verifierServerEndpoint = null)
    {
        string url;
        if (verifierServerEndpoint.IsNullOrEmpty())
        {
            var endPoint = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
            if (null == endPoint)
            {
                _logger.LogInformation("No Available Service Tips.{VerifierId}", input.VerifierId);
                return new ResponseResultDto<T>
                {
                    Success = false,
                    Message = "No Available Service Tips."
                };
            }
            url = endPoint + requestUri;
        }
        else
        {
            url = verifierServerEndpoint + requestUri;
        }
        
        var operationDetails =
            _accelerateManagerProvider.GenerateOperationDetails(input.OperationType, input.OperationDetails);
        var showOperationDetails = new ShowOperationDetailsDto
        {
            OperationType = GetOperationDecs(input.OperationType),
            Token = GetDetailDesc(input.OperationDetails, "symbol"),
            Amount = GetDetailDesc(input.OperationDetails, "amount"),
            Chain = GetChainDetailDesc(input.TargetChainId ?? input.ChainId),
            GuardianType = input.Type.ToString(),
            GuardianAccount = input.GuardianIdentifier,
            Time = DateTime.UtcNow + " UTC",
            IP = await GetIpDetailDesc(),
            ToAddress = GetDetailDesc(input.OperationDetails, "toAddress"),
            SingleLimit = GetDetailDesc(input.OperationDetails, "singleLimit"),
            DailyLimit = GetDetailDesc(input.OperationDetails, "dailyLimit")
        };
        await AmountHandler(showOperationDetails, input.OperationType, chainId:input.ChainId, symbol:showOperationDetails.Token,
            amount:showOperationDetails.Amount, singleLimit:showOperationDetails.SingleLimit, dailyLimit:showOperationDetails.DailyLimit);
        ToAddressHandler(showOperationDetails, showOperationDetails.ToAddress);
        var showOperationDetailsJson = JsonConvert.SerializeObject(showOperationDetails);
        var result = await GetResultFromVerifierAsync<T>(url, input.AccessToken, identifierHash, salt,
            input.OperationType,
            string.IsNullOrWhiteSpace(input.TargetChainId)
                ? ChainHelper.ConvertBase58ToChainId(input.ChainId).ToString()
                : ChainHelper.ConvertBase58ToChainId(input.TargetChainId).ToString(), operationDetails, input.SecondaryEmail, showOperationDetailsJson);
        return result;
    }

    private static void ToAddressHandler(ShowOperationDetailsDto showOperationDetails, string toAddress)
    {
        if (toAddress.IsNullOrEmpty())
        {
            return ;
        }

        var length = toAddress.Length / 2;
        showOperationDetails.ToAddress = string.Concat(toAddress.AsSpan(0, length), "\n", toAddress.AsSpan(length));
    }

    private async Task AmountHandler(ShowOperationDetailsDto showOperationDetailsDto, OperationType operationType,
        string chainId, string symbol, string amount, string singleLimit, string dailyLimit)
    {
        if (chainId.IsNullOrEmpty() || symbol.IsNullOrEmpty() || OperationType.GuardianApproveTransfer.Equals(operationType))
        {
            return ;
        }

        if (amount.IsNullOrEmpty() && singleLimit.IsNullOrEmpty() && dailyLimit.IsNullOrEmpty())
        {
            return;
        }
        var tokenInfoDto = await GetTokenInfoAsync(chainId, symbol);
        showOperationDetailsDto.Amount = CalculationHelper.GetAmountInUsd(amount, tokenInfoDto.Decimals);
        showOperationDetailsDto.SingleLimit = CalculationHelper.GetAmountInUsd(singleLimit, tokenInfoDto.Decimals);
        showOperationDetailsDto.DailyLimit = CalculationHelper.GetAmountInUsd(dailyLimit, tokenInfoDto.Decimals);
    }
    private async Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        IndexerTokens dto = null;
        try
        {
            dto = await _tokenProvider.GetTokenInfosAsync(chainId, symbol.Trim().ToUpper(), string.Empty, 0, 1);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "_tokenProvider GetTokenInfosAsync failed");
        }
        var tokenInfo = dto?.TokenInfo?.FirstOrDefault();
        if (tokenInfo == null)
        {
            return await _tokenCacheProvider.GetTokenInfoAsync(chainId, symbol, TokenType.Token);
        }

        return _objectMapper.Map<IndexerToken, GetTokenInfoDto>(tokenInfo);
    }

    private async Task<ResponseResultDto<T>> GetResultFromVerifierAsync<T>(string url,
        string accessToken, string identifierHash, string salt,
        OperationType verifierCodeOperationType, string chainId, string operationDetails, string secondaryEmail, string showOperationDetails)
    {
        var client = _httpClientFactory.CreateClient();
        var operationType = Convert.ToInt32(verifierCodeOperationType).ToString();
        var tokenParam = JsonConvert.SerializeObject(new
            { accessToken, identifierHash, salt, operationType, chainId, operationDetails, secondaryEmail, showOperationDetails });
        var param = new StringContent(tokenParam,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        string result = null;
        HttpResponseMessage response = null;
        try
        {
            response = await client.PostAsync(url, param);
            result = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e,"GetResultFromVerifierAsync post error url:{0} params:{1}", url, tokenParam);
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.LogError("{Message}", "Verifier return empty.");
            throw new UserFriendlyException("Verifier return empty.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}", $"Verifier fail: {result}");
            throw new UserFriendlyException(result);
        }

        _logger.LogInformation("Result from verifier: {result}", result);

        return JsonConvert.DeserializeObject<ResponseResultDto<T>>(result);
    }

    private string GetOperationDecs(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.CreateCAHolder => "Create AA Address",
            OperationType.SocialRecovery => "Social Recovery",
            OperationType.AddGuardian => "Add Guardian",
            OperationType.RemoveGuardian => "Remove Guardian",
            OperationType.UpdateGuardian => "Update Guardian",
            OperationType.RemoveOtherManagerInfo => "Remove Device",
            OperationType.SetLoginGuardian => "Set Login Account",
            OperationType.Approve => "Approve",
            OperationType.ModifyTransferLimit => "Set Transfer Limit",
            OperationType.GuardianApproveTransfer => "Guardian Approve Transfer",
            OperationType.UnSetLoginAccount => "Unset Login Account",
            _ => ""
        };
    }

    private string GetDetailDesc(string dtoOperationDetails, string keyword)
    {
        try
        {
            JsonConvert.DeserializeObject(dtoOperationDetails);
            var jsonObj = JObject.Parse(dtoOperationDetails);
            foreach (var child in jsonObj.Children())
            {
                if (child is not JProperty property)
                {
                    continue;
                }

                if (property.Name != keyword)
                {
                    continue;
                }

                var response = property.Value.ToString();
                if ("singleLimit".Equals(keyword) && "-1".Equals(response.Trim()))
                {
                    return "No Limit";
                }
                if ("dailyLimit".Equals(keyword) && "-1".Equals(response.Trim()))
                {
                    return "No Limit";
                }
                return response;
            }
        }
        catch (Exception e)
        {
            _logger.LogDebug("DeserializeObject Json failed : {json}, Error Message is : {message}",
                dtoOperationDetails, e.Message);
            return "";
        }

        return "";
    }

    private string GetChainDetailDesc(string chain)
    {
        var chainDetails = _chainOptions.ChainInfos.TryGetValue(chain, out var chainInfo);
        if (chainDetails)
        {
            var isMainChain = chainInfo.IsMainChain;
            return isMainChain ? "MainChain " + chainInfo.ChainId : "SideChain " + chainInfo.ChainId;
        }

        _logger.LogError("GetChainInfo Error:{chainId}", chain);
        return "";
    }

    private async Task<string> GetIpDetailDesc()
    {
        var ipInfo = await _ipInfoAppService.GetIpInfoAsync();
        return ipInfo.Country;
    }
}