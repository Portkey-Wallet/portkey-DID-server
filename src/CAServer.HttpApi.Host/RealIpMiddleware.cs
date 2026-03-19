using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.IpInfo;
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
    private readonly IIpWhiteListAppService _ipWhiteListAppService;
    private readonly AddToWhiteListUrlsOptions _addToWhiteListUrlsOptions;
    private readonly IHttpClientIpResolver _clientIpResolver;
    private readonly ICurrentUser _currentUser;


    public RealIpMiddleware(RequestDelegate requestDelegate, ILogger<RealIpMiddleware> logger,
        IIpWhiteListAppService ipWhiteListAppService,
        IOptions<AddToWhiteListUrlsOptions> addToWhiteListUrlsOptions, ICurrentUser currentUser,
        IHttpClientIpResolver clientIpResolver)
    {
        _requestDelegate = requestDelegate;
        _logger = logger;
        _ipWhiteListAppService = ipWhiteListAppService;
        _currentUser = currentUser;
        _addToWhiteListUrlsOptions = addToWhiteListUrlsOptions.Value;
        _clientIpResolver = clientIpResolver;
    }

    public async Task Invoke(HttpContext context)
    {
        var userIp = _clientIpResolver.GetForwardedClientIp();
        if (string.IsNullOrWhiteSpace(userIp))
        {
            _logger.LogDebug("Unknown ip address,Refused visit server.ipArr is null");
            await _requestDelegate(context);
            return;
        }
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
