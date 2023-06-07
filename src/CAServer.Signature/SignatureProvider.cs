using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Signature;

public interface ISignatureProvider
{
    Task<string> SignTxMsg(string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider, ISingletonDependency
{
    private readonly SignatureServerOptions _signatureServerOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SignatureProvider> _logger;


    public SignatureProvider(IOptionsSnapshot<SignatureServerOptions> signatureOptions,
        ILogger<SignatureProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _signatureServerOptions = signatureOptions.Value;
        _logger = logger;
    }

    public async Task<string> SignTxMsg(string publicKey, string hexMsg)
    {
        var signatureSend = new SendSignatureDto
        {
            PublicKey = publicKey,
            HexMsg = hexMsg,
        };
        var httpResult = await _httpClientFactory.CreateClient().SendAsync(new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_signatureServerOptions.BaseUrl),
            Content = new StringContent(JsonConvert.SerializeObject(signatureSend), Encoding.UTF8, "application/json")
        });
        if (httpResult.StatusCode == HttpStatusCode.OK)
        {
            return JsonConvert.DeserializeObject<SignResponseDto>(await httpResult.Content.ReadAsStringAsync())
                .Signature;
        }

        _logger.LogError("Will return empty because sign failed , PublicKey: {PublicKey}, HexMsg: {HexMsg}", publicKey,
            hexMsg);
        return "";
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