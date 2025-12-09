using CAServer.Cache;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Http;
using CAServer.SecurityServer.Dtos;
using CAServer.Signature.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using HttpMethod = System.Net.Http.HttpMethod;

namespace CAServer.Signature.Provider;

public interface ISecretProvider
{
    Task<string> GetSecretWithCacheAsync(string key);
    Task<string> GetAlchemyAesSignAsync(string key, string source);
    Task<string> GetAlchemyShaSignAsync(string key, string source);
    Task<string> GetAlchemyHmacSignAsync(string key, string source);
    Task<string> GetAppleAuthSignatureAsync(string key, string teamId, string clientId);
}

public class SecretProvider : ISecretProvider, ITransientDependency
{
    private const string GetSecurityUri = "/api/app/thirdPart/secret";
    private const string AlchemyPayAes = "/api/app/thirdPart/alchemyAes";
    private const string AlchemyPaySha = "/api/app/thirdPart/alchemySha";
    private const string AlchemyPayHmac = "/api/app/thirdPart/alchemyHmac";
    private const string AppleAuth = "/api/app/thirdPart/appleAuth";

    private readonly ILogger<SecretProvider> _logger;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOption;
    private readonly ILocalMemoryCache<string> _secretCache;
    private readonly IHttpProvider _httpProvider;

    public SecretProvider(ILogger<SecretProvider> logger, IOptionsMonitor<SignatureServerOptions> signatureOption,
        ILocalMemoryCache<string> secretCache, IHttpProvider httpProvider)
    {
        _logger = logger;
        _signatureOption = signatureOption;
        _secretCache = secretCache;
        _httpProvider = httpProvider;
    }

    private string Uri(string path)
    {
        return _signatureOption.CurrentValue.BaseUrl.TrimEnd('/') + path;
    }

    public async Task<string> GetSecretWithCacheAsync(string key)
    {
        return await _secretCache.GetOrAddAsync(key, async () => await GetSecretAsync(key), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration =
                DateTimeOffset.Now.AddSeconds(_signatureOption.CurrentValue.SecretCacheSeconds)
        });
    }

    private async Task<string> GetSecretAsync(string key)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<string>>(HttpMethod.Get,
            Uri(GetSecurityUri),
            param: new Dictionary<string, string>
            {
                ["key"] = key
            },
            header: SecurityServerHeader(key));
        _logger.LogError("GetSecretAsync key = {0} resp = {2}", key, JsonConvert.SerializeObject(resp));
        AssertHelper.NotEmpty(resp?.Data, "Secret response data empty");
        AssertHelper.IsTrue(resp!.Success, "Secret response failed {}", resp.Message);
        return EncryptionHelper.DecryptFromHex(resp.Data, _signatureOption.CurrentValue.AppSecret);
    }

    public async Task<string> GetAlchemyAesSignAsync(string key, string source)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<CommonThirdPartExecuteOutput>>(HttpMethod.Post,
            Uri(AlchemyPayAes),
            body: JsonConvert.SerializeObject(new CommonThirdPartExecuteInput
            {
                Key = key,
                BizData = source
            }, HttpProvider.DefaultJsonSettings),
            header: SecurityServerHeader(key, source));
        AssertHelper.NotNull(resp?.Data, "Signature response data empty");
        AssertHelper.IsTrue(resp!.Success, "Signature response failed {}", resp.Message);
        AssertHelper.NotEmpty(resp.Data.Value, "Signature empty");
        _logger.LogDebug("GetAlchemyAesSignAsync source={Source}, key={Key}, sign={Value}", source, key,
            resp.Data.Value);
        return resp.Data.Value;
    }

    public async Task<string> GetAlchemyShaSignAsync(string key, string source)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<CommonThirdPartExecuteOutput>>(HttpMethod.Post,
            Uri(AlchemyPaySha),
            body: JsonConvert.SerializeObject(new CommonThirdPartExecuteInput
            {
                Key = key,
                BizData = source
            }, HttpProvider.DefaultJsonSettings),
            header: SecurityServerHeader(key, source));
        AssertHelper.NotNull(resp?.Data, "Signature response data empty");
        AssertHelper.IsTrue(resp!.Success, "Signature response failed {}", resp.Message);
        AssertHelper.NotEmpty(resp.Data.Value, "Signature empty");
        _logger.LogDebug("GetAlchemyShaSignAsync source={Source}, key={Key}, sign={Value}", source, key,
            resp.Data.Value);
        return resp.Data.Value;
    }

    public async Task<string> GetAlchemyHmacSignAsync(string key, string source)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<CommonThirdPartExecuteOutput>>(HttpMethod.Post,
            Uri(AlchemyPayHmac),
            body: JsonConvert.SerializeObject(new CommonThirdPartExecuteInput
            {
                Key = key,
                BizData = source
            }, HttpProvider.DefaultJsonSettings),
            header: SecurityServerHeader(key, source));
        AssertHelper.NotNull(resp?.Data, "Signature response data empty");
        AssertHelper.IsTrue(resp!.Success, "Signature response failed {}", resp.Message);
        AssertHelper.NotEmpty(resp.Data.Value, "Signature empty");
        _logger.LogDebug("GetAlchemyHmacSignAsync source={Source}, key={Key}, sign={Value}", source, key,
            resp.Data.Value);
        return resp.Data.Value;
    }


    public async Task<string> GetAppleAuthSignatureAsync(string key, string teamId, string clientId)
    {
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<CommonThirdPartExecuteOutput>>(HttpMethod.Post,
            Uri(AppleAuth),
            body: JsonConvert.SerializeObject(new AppleAuthExecuteInput
            {
                Key = key,
                TeamId = teamId,
                ClientId = clientId
            }, HttpProvider.DefaultJsonSettings),
            header: SecurityServerHeader(key, teamId, clientId));
        AssertHelper.NotNull(resp?.Data, "Signature response data empty");
        AssertHelper.IsTrue(resp!.Success, "Signature response failed {}", resp.Message);
        AssertHelper.NotEmpty(resp.Data.Value, "Signature empty");
        _logger.LogDebug("GetAlchemyHmacSignAsync teamId={TeamId}, clientId={ClientId}, key={Key}, sign={Value}",
            teamId, clientId, key, resp.Data.Value);
        return resp.Data.Value;
    }


    public Dictionary<string, string> SecurityServerHeader(params string[] signValues)
    {
        var signString = string.Join(CommonConstant.EmptyString, signValues);
        return new Dictionary<string, string>
        {
            ["appid"] = _signatureOption.CurrentValue.AppId,
            ["signature"] = EncryptionHelper.EncryptHex(signString, _signatureOption.CurrentValue.AppSecret)
        };
    }
}