using System;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Http;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.IpInfo;

public class IpInfoClient : IIpInfoClient
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<IpServiceSettingOptions> _ipServiceSetting;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;
    private readonly ISecretProvider _secretProvider;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ILogger<IpInfoClient> _logger;


    public IpInfoClient(
        IOptionsMonitor<IpServiceSettingOptions> ipServiceSettingOption,
        ISecretProvider secretProvider, IHttpProvider httpProvider,
        IOptionsMonitor<SignatureServerOptions> signatureOptions, IHttpClientProvider httpClientProvider,
        ILogger<IpInfoClient> logger)
    {
        _secretProvider = secretProvider;
        _httpProvider = httpProvider;
        _signatureOptions = signatureOptions;
        _httpClientProvider = httpClientProvider;
        _logger = logger;
        _ipServiceSetting = ipServiceSettingOption;
    }

    public async Task<IpInfoDto> GetIpInfoAsync(string ip)
    {
        var accessKey = await _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.IpService);
        var requestUrl = _ipServiceSetting.CurrentValue.BaseUrl.TrimEnd('/') + "/" + ip;
        requestUrl += $"?access_key={accessKey}&language={_ipServiceSetting.CurrentValue.Language}";
        var response = await _httpProvider.InvokeAsync<IpInfoDto>(HttpMethod.Get, requestUrl);
        AssertHelper.IsTrue(response.Error == null, response.Error?.Info ?? "Get ip info failed {}", ip);
        return response;
    }

    public async Task<IpInfoDto> GetCountryInfoAsync(string ip)
    {
        try
        {
            var requestUrl = $"{_ipServiceSetting.CurrentValue.BaseUrl.TrimEnd('/')}/{ip}";
            requestUrl +=
                $"?access_key={_ipServiceSetting.CurrentValue.HolderStatisticAccessKey}&language={_ipServiceSetting.CurrentValue.Language}";
            return await _httpClientProvider.GetAsync<IpInfoDto>(requestUrl);
        }
        catch (Exception e)
        {
            _logger.LogError("get ip info error, ip:{ip}", ip);
            return null;
        }
    }
}