using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp;

namespace CAServer.IpInfo;

public class IpInfoClient : IIpInfoClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IpServiceSettingOptions _ipServiceSetting;
    private readonly ILogger<IpInfoClient> _logger;

    public IpInfoClient(IHttpClientFactory httpClientFactory,
        IOptions<IpServiceSettingOptions> ipServiceSettingOption,
        ILogger<IpInfoClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ipServiceSetting = ipServiceSettingOption.Value;
    }

    public async Task<IpInfoDto> GetIpInfoAsync(string ip)
    {
        _logger.LogError($"#### ip is {ip}");
        var requestUrl = $"{_ipServiceSetting.BaseUrl.TrimEnd('/')}/{ip}";
        requestUrl += $"?access_key={_ipServiceSetting.AccessKey}&language={_ipServiceSetting.Language}";

        var httpClient = _httpClientFactory.CreateClient();
        var httpResponseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUrl));

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}",
                $"Request for ip info error: {((int)httpResponseMessage.StatusCode).ToString()}");
            throw new UserFriendlyException("Request error.", ((int)httpResponseMessage.StatusCode).ToString());
        }

        var content = await httpResponseMessage.Content.ReadAsStringAsync();

        if (content.Contains("error"))
        {
            _logger.LogError("{Message}", $"Request for ip info error: {content}");
            throw new UserFriendlyException(JObject.Parse(content)["error"]?["info"]?.ToString());
        }

        return JsonConvert.DeserializeObject<IpInfoDto>(content);
    }
}