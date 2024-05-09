using System;
using System.Collections.Generic;
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
using CAServer.Settings;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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


    public VerifierServerClient(IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions,
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<VerifierServerClient> logger,
        IHttpClientFactory httpClientFactory, IAccelerateManagerProvider accelerateManagerProvider,
        IOptions<ChainOptions> chainOptions, IIpInfoAppService ipInfoAppService)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, httpClientFactory, true);
        _httpClientFactory = httpClientFactory;
        _accelerateManagerProvider = accelerateManagerProvider;
        _ipInfoAppService = ipInfoAppService;
        _chainOptions = chainOptions.Value;
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
            Chain = GetChainDetailDesc(dto.ChainId),
            GuardianType = dto.Type,
            GuardianAccount = dto.GuardianIdentifier,
            Time = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
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

        return await GetResultFromVerifierAsync<T>(url, input.AccessToken, identifierHash, salt,
            input.OperationType,
            string.IsNullOrWhiteSpace(input.TargetChainId)
                ? ChainHelper.ConvertBase58ToChainId(input.ChainId).ToString()
                : ChainHelper.ConvertBase58ToChainId(input.TargetChainId).ToString(), operationDetails);
    }

    private async Task<ResponseResultDto<T>> GetResultFromVerifierAsync<T>(string url,
        string accessToken, string identifierHash, string salt,
        OperationType verifierCodeOperationType, string chainId, string operationDetails)
    {
        var client = _httpClientFactory.CreateClient();
        var operationType = Convert.ToInt32(verifierCodeOperationType).ToString();
        var tokenParam = JsonConvert.SerializeObject(new
            { accessToken, identifierHash, salt, operationType, chainId, operationDetails });
        var param = new StringContent(tokenParam,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var response = await client.PostAsync(url, param);
        var result = await response.Content.ReadAsStringAsync();

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
            OperationType.RemoveOtherManagerInfo => "Remove device",
            OperationType.SetLoginGuardian => "Set login account",
            OperationType.Approve => "Approve",
            OperationType.ModifyTransferLimit => "Set Transfer Limit",
            OperationType.GuardianApproveTransfer => "Guardian Approve Transfer",
            OperationType.UnSetLoginAccount => "Unset login account",
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