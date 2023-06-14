using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Signature;
using CAServer.Verifier;
using Castle.Core.Logging;
using CAVerifierServer.IpWhiteList;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Core;
using Volo.Abp.DependencyInjection;

namespace CAServer.IpWhiteList;

public class IpWhiteListAppService : IIpWhiteListAppService,ISingletonDependency
{
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AddToWhiteListUrlsOptions _addToWhiteListUrlsOptions;
    private readonly ILogger<IpWhiteListAppService> _logger;

    public IpWhiteListAppService(IHttpClientFactory httpClientFactory, IOptionsSnapshot<AddToWhiteListUrlsOptions> addToWhiteListUrlsOptions, ILogger<IpWhiteListAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _addToWhiteListUrlsOptions = addToWhiteListUrlsOptions.Value;
    }


    public Task<List<string>> GetIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
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
                response = JsonConvert.DeserializeObject<ResponseResultDto<AddUserIpToWhiteListResponseDto>>(await httpResult.Content.ReadAsStringAsync()).Data.IsInWhiteList;
            }
            return response;
        }
        catch (Exception e)
        {
            _logger.LogError("IsInWhiteListAsync error: {error}",e.Message);
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
                var response = JsonConvert.DeserializeObject<ResponseResultDto<AddUserIpToWhiteListResponseDto>>(await httpResult.Content.ReadAsStringAsync())
                    .Success;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("AddIpWhiteListAsync error: {error}",e.Message);
            throw;
        }
        


    }

    public Task RemoveIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task UpdateIpWhiteListAsync()
    {
        throw new System.NotImplementedException();
    }
}

public class AddUserIpToWhiteListResponseDto
{
    public bool IsInWhiteList { get; set; }

    
}
