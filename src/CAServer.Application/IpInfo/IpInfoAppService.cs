using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.IpInfo;

[RemoteService(false), DisableAuditing]
public class IpInfoAppService : CAServerAppService, IIpInfoAppService
{
    private readonly IIpInfoClient _ipInfoClient;
    private readonly DefaultIpInfoOptions _defaultIpInfoOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache<IpInfoResultDto> _distributedCache;
    private readonly IpServiceSettingOptions _ipServiceSettingOptions;
    private readonly string _prefix = "IpInfo-";

    public IpInfoAppService(IIpInfoClient ipInfoClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<DefaultIpInfoOptions> defaultIpInfoOptions,
        IOptions<IpServiceSettingOptions> ipServiceSettingOptions,
        IDistributedCache<IpInfoResultDto> distributedCache
    )
    {
        _ipInfoClient = ipInfoClient;
        _httpContextAccessor = httpContextAccessor;
        _distributedCache = distributedCache;
        _defaultIpInfoOptions = defaultIpInfoOptions.Value;
        _ipServiceSettingOptions = ipServiceSettingOptions.Value;
    }

    public async Task<IpInfoResultDto> GetIpInfoAsync()
    {
        try
        {
            var ip = GetIp();
            var ipInfoResult = await _distributedCache.GetAsync(_prefix + ip);

            if (ipInfoResult != null) return ipInfoResult;

            var ipInfoDto = await _ipInfoClient.GetIpInfoAsync(ip);
            var ipInfo = ObjectMapper.Map<IpInfoDto, IpInfoResultDto>(ipInfoDto);

            if (string.IsNullOrEmpty(ipInfo.Country))
            {
                return ObjectMapper.Map<DefaultIpInfoOptions, IpInfoResultDto>(_defaultIpInfoOptions);
            }

            await _distributedCache.SetAsync(_prefix + ip, ipInfo);
            return ipInfo;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Get ipInfo fail.");
            return ObjectMapper.Map<DefaultIpInfoOptions, IpInfoResultDto>(_defaultIpInfoOptions);
        }
    }

    private string GetIp()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers?.ContainsKey("X-Forwarded-For") == false)
        {
            throw new UserFriendlyException("Not set nginx header.");
        }

        Logger.LogInformation(
            $"X-Forwarded-For: {_httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].ToString()}");

        var ip = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].ToString().Split(',')
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new UserFriendlyException("Unknown ip address. ip is empty.");
        }

        return ip;
    }
}