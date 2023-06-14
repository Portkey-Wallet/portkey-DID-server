using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CAServer.IpWhiteList;
using CAServer.IpWhiteList.Dtos;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Users;

namespace CAServer;

public class RealIpMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ILogger<RealIpMiddleware> _logger;
    private readonly RealIpOptions _realIpOptions;
    private const string LocalIpaddress = "127.0.0.1";
    private readonly IIpWhiteListAppService _ipWhiteListAppService;
    private readonly AddToWhiteListUrlsOptions _addToWhiteListUrlsOptions;
    private readonly ICurrentUser _currentUser;
    private const string Version = "version";
    private const string CurrentVersion = "v1.3.0";

    public RealIpMiddleware(RequestDelegate requestDelegate, IOptions<RealIpOptions> realIpOptions,
        ILogger<RealIpMiddleware> logger, IIpWhiteListAppService ipWhiteListAppService,
        IOptions<AddToWhiteListUrlsOptions> addToWhiteListUrlsOptions, ICurrentUser currentUser)
    {
        _requestDelegate = requestDelegate;
        _logger = logger;
        _ipWhiteListAppService = ipWhiteListAppService;
        _currentUser = currentUser;
        _addToWhiteListUrlsOptions = addToWhiteListUrlsOptions.Value;
        _realIpOptions = realIpOptions.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.ContainsKey(Version) || headers[Version].ToString() != CurrentVersion)
        {
            await _requestDelegate(context);
            return;
        }

        if (!headers.ContainsKey(_realIpOptions.HeaderKey))
        {
            throw new ExternalException("Unknown ip address. no setting");
        }

        var ipArr = headers["X-Forwarded-For"].ToString().Split(',');
        if (ipArr.Length == 0)
        {
            _logger.LogDebug("Unknown ip address");
            throw new ExternalException("Unknown ip address. Refused visit server.ipArr is null");
        }

        var userIp = ipArr[0].Trim();
        var userId = _currentUser.Id ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            _logger.LogDebug("Unknown user id");
            await _requestDelegate(context);
            return;
        }


        var requestDto = new AddUserIpToWhiteListRequestDto(
        );
        var checkUrls = _addToWhiteListUrlsOptions.Urls;
        try
        {
            if (context.Request.Path.Value != null)
            {
                var path = context.Request.Path.Value;
                if (checkUrls.Contains(path))
                {
                    requestDto.UserIp = userIp;
                    requestDto.UserId = userId;
                    await _ipWhiteListAppService.AddIpWhiteListAsync(requestDto);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AddIpToWhiteList error:{error}",e.Message);
        }
        await _requestDelegate(context);
    }
}