using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAServer.Commons;
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

    private readonly string _ipPattern =
        @"^([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))(\.([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))){3}$";

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
        var ip = string.Empty;
        try
        {
            ip = GetIp();
            var ipInfoResult = await _distributedCache.GetAsync(_prefix + ip);

            if (ipInfoResult != null) return ipInfoResult;

            var ipInfoDto = await _ipInfoClient.GetIpInfoAsync(ip);
            var ipInfo = ObjectMapper.Map<IpInfoDto, IpInfoResultDto>(ipInfoDto);

            if (string.IsNullOrEmpty(ipInfo.Country))
            {
                return ObjectMapper.Map<DefaultIpInfoOptions, IpInfoResultDto>(_defaultIpInfoOptions);
            }

            await _distributedCache.SetAsync(_prefix + ip, ipInfo, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            });
            return ipInfo;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Get ipInfo fail, ip is {ip}", ip.IsNullOrWhiteSpace() ? "empty" : ip);
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

        ip ??= string.Empty;

        if (!Match(ip))
        {
            throw new UserFriendlyException($"Unknown ip address: {ip}.");
        }

        return ip;
    }

    private bool Match(string ip) => new Regex(_ipPattern).IsMatch(ip);

    public string GetRemoteIp()
    {
        var clientIp = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return clientIp.Contains(',') ? clientIp.Split(",")[0].Trim() : clientIp;
        }

        var remoteIpAddress = _httpContextAccessor?.HttpContext?.Request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return string.Empty;
        }

        return remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4().ToString()
            : remoteIpAddress.MapToIPv6().ToString();
    }

    public string GetRemoteIp(string random)
    {
        return !random.IsNullOrEmpty() ? random : GetRemoteIp();
    }
}