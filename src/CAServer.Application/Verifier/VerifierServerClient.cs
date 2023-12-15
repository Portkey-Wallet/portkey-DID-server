using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Common;
using CAServer.Dtos;
using CAServer.Monitor;
using CAServer.Settings;
using CAServer.Switch;
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
    private readonly IIndicatorScope _indicatorScope;


    public VerifierServerClient(IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions,
        IGetVerifierServerProvider getVerifierServerProvider,
        ILogger<VerifierServerClient> logger,
        IHttpClientFactory httpClientFactory,
        IIndicatorScope indicatorScope)
    {
        _getVerifierServerProvider = getVerifierServerProvider;
        _logger = logger;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, httpClientFactory, true);
        _httpClientFactory = httpClientFactory;
        _indicatorScope = indicatorScope;
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
        var indicator = _indicatorScope.Begin(MonitorTag.SendVerificationRequestAsync,"GetVerifierServerEndPointsAsync");
        var endPoint = await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(dto.VerifierId, dto.ChainId);
        _indicatorScope.End(indicator);
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
        indicator = _indicatorScope.Begin(MonitorTag.SendVerificationRequestAsync,"sendVerificationRequest");
        var url = endPoint + "/api/app/account/sendVerificationRequest";
        var parameters = new Dictionary<string, string>
        {
            { "type", dto.Type },
            { "guardianIdentifier", dto.GuardianIdentifier },
            { "verifierSessionId", dto.VerifierSessionId.ToString() },
        };
        ResponseResultDto<VerifierServerResponse> verifierServerResponse = null;
        try
        {
            verifierServerResponse = await _httpService.PostResponseAsync<ResponseResultDto<VerifierServerResponse>>(url, parameters);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "send verification request error:{0}", JsonConvert.SerializeObject(parameters));
            throw;
        }
        _indicatorScope.End(indicator);
        return verifierServerResponse;
    }

    public async Task<ResponseResultDto<VerificationCodeResponse>> VerifyCodeAsync(VierifierCodeRequestInput input)
    {
        var indicator = _indicatorScope.Begin(MonitorTag.VerifyCodeAsync,"GetVerifierServerEndPointsAsync");
        var endPoint =
            await _getVerifierServerProvider.GetVerifierServerEndPointsAsync(input.VerifierId, input.ChainId);
        _indicatorScope.End(indicator);
        if (null == endPoint)
        {
            _logger.LogInformation("No Available Service Tips.{VerifierId}", input.VerifierId);
            return new ResponseResultDto<VerificationCodeResponse>
            {
                Success = false,
                Message = "No Available Service Tips."
            };
        }


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
            }
        };

        indicator = _indicatorScope.Begin(MonitorTag.VerifyCodeAsync,"verifyCode");
        var responseAsync =
            await _httpService.PostResponseAsync<ResponseResultDto<VerificationCodeResponse>>(url, parameters);
        _indicatorScope.End(indicator);
        return responseAsync;
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


        return await GetResultFromVerifierAsync<T>(url, input.AccessToken, identifierHash, salt,
            input.OperationType,
            string.IsNullOrWhiteSpace(input.TargetChainId)
                ? ChainHelper.ConvertBase58ToChainId(input.ChainId).ToString()
                : ChainHelper.ConvertBase58ToChainId(input.TargetChainId).ToString());
    }

    private async Task<ResponseResultDto<T>> GetResultFromVerifierAsync<T>(string url,
        string accessToken, string identifierHash, string salt,
        OperationType verifierCodeOperationType, string chainId)
    {
        var client = _httpClientFactory.CreateClient();
        var operationType = Convert.ToInt32(verifierCodeOperationType).ToString();
        var tokenParam = JsonConvert.SerializeObject(new { accessToken, identifierHash, salt, operationType, chainId });
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