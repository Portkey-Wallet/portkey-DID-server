using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.IpWhiteList.Dtos;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.IpWhiteList;

public class IpWhiteListAppService : IIpWhiteListAppService, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AddToWhiteListUrlsOptions _addToWhiteListUrlsOptions;
    private readonly ILogger<IpWhiteListAppService> _logger;

    public IpWhiteListAppService(IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<AddToWhiteListUrlsOptions> addToWhiteListUrlsOptions, ILogger<IpWhiteListAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _addToWhiteListUrlsOptions = addToWhiteListUrlsOptions.Value;
    }


    public async Task<bool> IsInWhiteListAsync(string userIpAddress)
    {
        var requestDto = new CheckUserIpInWhiteListRequestDto
        {
            Ip = userIpAddress
        };
        var response = false;
        try
        {
            var httpResult = await _httpClientFactory.CreateClient().SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_addToWhiteListUrlsOptions.BaseCheckUrl),
                Content = new StringContent(JsonConvert.SerializeObject(requestDto), Encoding.UTF8, "application/json")
            });
            if (httpResult.StatusCode == HttpStatusCode.OK)
            {
                response = JsonConvert
                    .DeserializeObject<ResponseResultDto<CheckUserIpInWhiteListResponseDto>>(
                        await httpResult.Content.ReadAsStringAsync()).Data.IsInWhiteList;
            }
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError("IsInWhiteListAsync error: {error}", e.Message);
            return false;
        }
    }

    public async Task AddIpWhiteListAsync(AddUserIpToWhiteListRequestDto requestDto)
    {
        try
        {
            var httpResult = await _httpClientFactory.CreateClient().SendAsync(new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_addToWhiteListUrlsOptions.BaseAddUrl),
                Content = new StringContent(JsonConvert.SerializeObject(requestDto), Encoding.UTF8, "application/json")
            });
            if (httpResult.StatusCode == HttpStatusCode.OK)
            {
                var response = JsonConvert
                    .DeserializeObject<ResponseResultDto<AddUserIpToWhiteListResponseDto>>(
                        await httpResult.Content.ReadAsStringAsync())
                    .Success;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("AddIpWhiteListAsync error: {error}", e.Message);
            throw;
        }
    }
}

public class AddUserIpToWhiteListResponseDto
{
    public bool Succsee { get; set; }
}

public class CheckUserIpInWhiteListResponseDto
{
    public bool IsInWhiteList { get; set; }
}