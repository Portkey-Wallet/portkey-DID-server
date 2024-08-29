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
using CAServer.Verifier.Dtos;
using CAVerifierServer.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

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

    public VerifierServerClient(IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions,
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<VerifierServerClient> logger,
        IHttpClientFactory httpClientFactory, IAccelerateManagerProvider accelerateManagerProvider,
        IOptions<ChainOptions> chainOptions, IIpInfoAppService ipInfoAppService,
        IContractProvider contractProvider)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, httpClientFactory, true);
        _httpClientFactory = httpClientFactory;
        _accelerateManagerProvider = accelerateManagerProvider;
        _ipInfoAppService = ipInfoAppService;
        _chainOptions = chainOptions.Value;
        _contractProvider = contractProvider;
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
            Time = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture),
            IP = await GetIpDetailDesc()
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
        var endPoint = await _getVerifierServerProvider.GetRandomVerifierServerEndPointAsync(chainInfo.ChainId);
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

    public async Task<bool> SendNotificationAfterApprovalAsync(ManagerApprovedDto managerApprovedDto, string email)
    {
        var endPoint = await _getVerifierServerProvider.GetRandomVerifierServerEndPointAsync(managerApprovedDto.ChainId);
        _logger.LogInformation("EndPiont is {endPiont} :", endPoint);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{0}", managerApprovedDto.ChainId);
            return false;
        }
        var showOperationDetails = new ShowOperationDetailsDto
        {
            // OperationType = ,
            Token = managerApprovedDto.Symbol,
            Amount = managerApprovedDto.Amount.ToString(),
            Chain = managerApprovedDto.ChainId,
            // GuardianType = dto.Type,
            // GuardianAccount = dto.GuardianIdentifier,
            Time = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture),
            IP = await GetIpDetailDesc()
        };

        var showOperationDetailsJson = JsonConvert.SerializeObject(showOperationDetails);

        var url = endPoint + "/api/app/account/sendNotification";
        var parameters = new Dictionary<string, string>
        {
            { "template", ((int)EmailTemplate.AfterApproval).ToString() },
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
        _logger.LogInformation("VerifySecondaryEmailCodeAsync url:{0} parameters:{1}", url, parameters.ToString());
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
        string requestUri, string identifierHash, string salt)
    {
        var endPoint =
            await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{VerifierId}", input.VerifierId);
            return new ResponseResultDto<T>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }

        var url = endPoint + requestUri;
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
            Time = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture),
            IP = await GetIpDetailDesc(),
            ToAddress = GetDetailDesc(input.OperationDetails, "toAddress"),
            SingleLimit = GetDetailDesc(input.OperationDetails, "singleLimit"),
            DailyLimit = GetDetailDesc(input.OperationDetails, "dailyLimit")
        };
        var showOperationDetailsJson = JsonConvert.SerializeObject(showOperationDetails);
        //todo for test, remove bofore online
        _logger.LogDebug("GetResultFromVerifier before request url:{0} accessToken:{1} identifierHash:{2} salt:{3}" +
                         "operationType:{4} chainId:{5} secondaryEmail:{6} showOperationDetailsJson:{7}", 
            url, input.AccessToken, identifierHash, salt, input.OperationType, input.ChainId, input.SecondaryEmail, showOperationDetailsJson);
        var result = await GetResultFromVerifierAsync<T>(url, input.AccessToken, identifierHash, salt,
            input.OperationType,
            string.IsNullOrWhiteSpace(input.TargetChainId)
                ? ChainHelper.ConvertBase58ToChainId(input.ChainId).ToString()
                : ChainHelper.ConvertBase58ToChainId(input.TargetChainId).ToString(), operationDetails, input.SecondaryEmail, showOperationDetailsJson);
        _logger.LogDebug("GetResultFromVerifierAsync url:{0} secondaryEmail:{1} result:{2}", url, input.SecondaryEmail, JsonConvert.SerializeObject(result));
        return result;
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