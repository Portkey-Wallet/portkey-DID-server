using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.TwitterAuth.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.TwitterAuth;

[RemoteService(false), DisableAuditing]
public class TwitterAuthAppService : CAServerAppService, ITwitterAuthAppService
{
    private readonly IHttpClientService _httpClientService;
    private readonly TwitterAuthOptions _options;

    public TwitterAuthAppService(IHttpClientService httpClientService, IOptionsSnapshot<TwitterAuthOptions> options)
    {
        _httpClientService = httpClientService;
        _options = options.Value;
    }

    public async Task ReceiveAsync(TwitterAuthDto twitterAuthDto)
    {
        Logger.LogInformation("receive twitter callback, data: {data}", JsonConvert.SerializeObject(twitterAuthDto));
        if (twitterAuthDto.Code.IsNullOrEmpty())
        {
            throw new UserFriendlyException("auth code is empty");
        }

        var basicAuth = GetBasicAuth(_options.ClientId, _options.ClientSecret);
        var requestParam = new Dictionary<string, string>
        {
            ["code"] = twitterAuthDto.Code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUrl, 
            ["client_id"] = _options.ClientId,
            ["code_verifier"] = "challenge"
        };

        var header = new Dictionary<string, string>
        {
            ["Authorization"] = basicAuth
        };

        var response = await _httpClientService.PostAsync<string>(_options.TwitterTokenUrl, RequestMediaType.Form,
            requestParam,
            header);

        Logger.LogInformation("send code to twitter success, response:{response}", response);
    }

    private string GetBasicAuth(string clientId, string clientSecret)
    {
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        return $"Basic {basicToken}";
    }
}