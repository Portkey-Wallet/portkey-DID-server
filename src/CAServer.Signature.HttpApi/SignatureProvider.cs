using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.Signature.Dtos;
using Newtonsoft.Json;

namespace CAServer.Signature;

public interface ISignatureProvider
{
    Task<string> SignTransaction(string uri, string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider
{
    public async Task<string> SignTransaction(string uri, string publicKey, string hexMsg)
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
            RequestUri = new Uri(uri),
            Content = new StringContent(JsonConvert.SerializeObject(signatureSend), Encoding.UTF8, "application/json")
        });
        if (httpResult.StatusCode == HttpStatusCode.OK)
        {
            return JsonConvert.DeserializeObject<SignatureResponseDto>(await httpResult.Content.ReadAsStringAsync())
                .Signature;
        }

        return "";
    }
}