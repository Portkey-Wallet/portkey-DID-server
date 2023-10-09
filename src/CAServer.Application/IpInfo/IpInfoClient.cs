using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
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
    private readonly IIndicatorScope _indicatorScope;

    public IpInfoClient(IHttpClientFactory httpClientFactory,
        IOptions<IpServiceSettingOptions> ipServiceSettingOption,
        ILogger<IpInfoClient> logger, IIndicatorScope indicatorScope)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _indicatorScope = indicatorScope;
        _ipServiceSetting = ipServiceSettingOption.Value;
    }

    public async Task<IpInfoDto> GetIpInfoAsync(string ip)
    {
        var requestUrl = $"{_ipServiceSetting.BaseUrl.TrimEnd('/')}/{ip}";
        var target = MonitorHelper.GetHttpTarget(MonitorRequestType.GetIpInfo, requestUrl);
        requestUrl += $"?access_key={_ipServiceSetting.AccessKey}&language={_ipServiceSetting.Language}";

        var interIndicator = _indicatorScope.Begin(MonitorTag.Http, target);
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

        _indicatorScope.End(interIndicator);
        return JsonConvert.DeserializeObject<IpInfoDto>(content);
    }
}