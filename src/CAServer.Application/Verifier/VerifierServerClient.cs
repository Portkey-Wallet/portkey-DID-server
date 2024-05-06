using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Accelerate;
using CAServer.Common;
using CAServer.Dtos;
using CAServer.Settings;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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


    public VerifierServerClient(IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions,
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<VerifierServerClient> logger,
        IHttpClientFactory httpClientFactory, IAccelerateManagerProvider accelerateManagerProvider)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, httpClientFactory, true);
        _httpClientFactory = httpClientFactory;
        _accelerateManagerProvider = accelerateManagerProvider;
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
        var url = endPoint + "/api/app/account/sendVerificationRequest";
        var parameters = new Dictionary<string, string>
        {
            { "type", dto.Type },
            { "guardianIdentifier", dto.GuardianIdentifier },
            { "verifierSessionId", dto.VerifierSessionId.ToString() },
            { "operationDetails", operationDetails },
            { "showOperationDetails", dto.OperationDetails }
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

    public async Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(VerifyTokenRequestDto input, string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyTelegramToken";
        return await GetResultAsync<VerifyTokenDto<TelegramUserExtraInfo>>(input, requestUri, identifierHash, salt);
    }

    public async Task<ResponseResultDto<VerificationCodeResponse>> VerifyFacebookTokenAsync(VerifyTokenRequestDto requestDto, string identifierHash, string salt)
    {
        var requestUri = "/api/app/account/verifyFacebookToken";
        return await GetResultAsync<VerificationCodeResponse>(requestDto, requestUri, identifierHash, salt);
    }

    public async Task<ResponseResultDto<VerifyFacebookUserInfoDto>> VerifyFacebookAccessTokenAsync(VerifyTokenRequestDto input)
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

    public async Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyTwitterTokenAsync(VerifyTokenRequestDto input, string identifierHash, string salt)
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
}