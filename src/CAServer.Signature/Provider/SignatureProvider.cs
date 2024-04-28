using CAServer.Common;
using CAServer.Commons;
using CAServer.Http;
using CAServer.Signature.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Signature.Provider;

public interface ISignatureProvider
{
    Task<string> SignTxMsg(string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider, ISingletonDependency
{
    private const string GetSecurityUri = "/api/app/signature";

    private readonly IOptionsMonitor<SignatureServerOptions> _signatureServerOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly ILogger<SignatureProvider> _logger;
    


    public SignatureProvider(IOptionsMonitor<SignatureServerOptions> signatureOptions,
        ILogger<SignatureProvider> logger, IHttpProvider httpProvider)
    {
        _signatureServerOptions = signatureOptions;
        _logger = logger;
        _httpProvider = httpProvider;
    }

    private string Uri(string path)
    {
        return _signatureServerOptions.CurrentValue.BaseUrl.TrimEnd('/') + path;
    }

    public async Task<string> SignTxMsg(string publicKey, string hexMsg)
    {
        var signatureSend = new SendSignatureDto
        {
            PublicKey = publicKey,
            HexMsg = hexMsg,
        };

        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<SignResponseDto>>(HttpMethod.Post,
            Uri(GetSecurityUri), 
            body: JsonConvert.SerializeObject(signatureSend),
            header: SecurityServerHeader()
            );
        AssertHelper.IsTrue(resp?.Success ?? false, "Signature response failed");
        AssertHelper.NotEmpty(resp!.Data?.Signature, "Signature response empty");
        return resp.Data!.Signature;
    }
    
    public Dictionary<string, string> SecurityServerHeader(params string[] signValues)
    {
        var signString = string.Join(CommonConstant.EmptyString, signValues);
        return new Dictionary<string, string>
        {
            ["appid"] = _signatureServerOptions.CurrentValue.AppId,
            ["signature"] = EncryptionHelper.EncryptHex(signString, _signatureServerOptions.CurrentValue.AppSecret)
        };
    }
}

public class SendSignatureDto
{
    public string PublicKey { get; set; }
    public string HexMsg { get; set; }
}

public class SignResponseDto
{
    public string Signature { get; set; }
}