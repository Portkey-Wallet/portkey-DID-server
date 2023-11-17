using System;
using System.Linq;
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
    private readonly IIpWhiteListAppService _ipWhiteListAppService;
    private readonly AddToWhiteListUrlsOptions _addToWhiteListUrlsOptions;
    private readonly ICurrentUser _currentUser;


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
        if (!headers.ContainsKey(_realIpOptions.HeaderKey))
        {
            _logger.LogDebug("Unknown ip address. no setting");
            await _requestDelegate(context);
            return;
        }

        var ipArr = headers["X-Forwarded-For"].ToString().Split(',');
        if (ipArr.Length == 0)
        {
            _logger.LogDebug("Unknown ip address,Refused visit server.ipArr is null");
            await _requestDelegate(context);
            return;
        }

        var userIp = ipArr[0].Trim();
        var userId = _currentUser.Id ?? Guid.Empty;
        _logger.LogDebug("current user id is {id}", userId);
        if (userId == Guid.Empty)
        {
            _logger.LogDebug("Unknown user id");
            await _requestDelegate(context);
            return;
        }


        var requestDto = new AddUserIpToWhiteListRequestDto(
        );
        var checkUrls = _addToWhiteListUrlsOptions.Urls.Distinct();
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
            _logger.LogError("AddIpToWhiteList error:{error}", e);
        }
        await _requestDelegate(context);
    }
}