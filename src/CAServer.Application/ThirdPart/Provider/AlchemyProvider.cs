using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;

public interface IAlchemyProvider
{
    Task<string> HttpGetFromAlchemy(string path);
    Task<string> HttpPost2AlchemyAsync(string path, string inputStr);
}

public class AlchemyProvider : IAlchemyProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;

    public AlchemyProvider(IHttpClientFactory httpClientFactory,
        IOptions<ThirdPartOptions> merchantOptions,
        ILogger<AlchemyServiceAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _logger = logger;
    }


    public async Task<string> HttpGetFromAlchemy(string path)
    {
        var client = _httpClientFactory.CreateClient();
        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.GetAsync(_alchemyOptions.BaseUrl + path);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.LogInformation("[ACH][{StatusCode}][get]request url: \n{url}", respMsg.StatusCode,
            _alchemyOptions.BaseUrl + path);

        return respStr;
    }

    public async Task<string> HttpPost2AlchemyAsync(string path, string inputStr)
    {
        _logger.LogInformation("[ACH]send request body : \n{requestBody}", inputStr);

        StringContent str2Json = new StringContent(inputStr, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();

        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.PostAsync(_alchemyOptions.BaseUrl + path, str2Json);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.LogInformation("[ACH][{StatusCode}][post]request url: \n{url}, body :{respStr}", respMsg.StatusCode,
            _alchemyOptions.BaseUrl + path, respStr);

        return respStr;
    }

    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        string timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var sign = GenerateAlchemyApiSign(timeStamp);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", _alchemyOptions.AppId,
            timeStamp, sign);

        client.DefaultRequestHeaders.Add("appId", _alchemyOptions.AppId);
        client.DefaultRequestHeaders.Add("timestamp", timeStamp);
        client.DefaultRequestHeaders.Add("sign", sign);
    }

    // Generate Alchemy request sigh by "appId + appSecret + timestamp".
    private string GenerateAlchemyApiSign(string timeStamp)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_alchemyOptions.AppId + _alchemyOptions.AppSecret + timeStamp);
        byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

        StringBuilder sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("X2"));
        }

        return sb.ToString().ToLower();
    }
}