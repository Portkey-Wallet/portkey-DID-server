using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface ISignatureProvider
{
    Task<string> SignTxMsg(string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider
{
    private readonly SignatureOptions _signatureOptions;

    public SignatureProvider(SignatureOptions signatureOptions)
    {
        _signatureOptions = signatureOptions;
    }

    public async Task<string> SignTxMsg(string publicKey, string hexMsg)
    {
        var signatureSend = new SendSignatureDto
        {
            PublicKey = publicKey,
            HexMsg = hexMsg,
        };
        var httpClient = new HttpClient();
        var httpResult = await httpClient.SendAsync(new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_signatureOptions.BaseUrl),
            Content = new StringContent(JsonConvert.SerializeObject(signatureSend), Encoding.UTF8, "application/json")
        });
        if (httpResult.StatusCode == HttpStatusCode.OK)
        {
            return JsonConvert.DeserializeObject<SignResponseDto>(await httpResult.Content.ReadAsStringAsync())
                .Signature;
        }

        return "";
    }
}

public class SignResponseDto
{
    public string Signature { get; set; }
}

public class SendSignatureDto
{
    public string PublicKey { get; set; }
    public string HexMsg { get; set; }
}