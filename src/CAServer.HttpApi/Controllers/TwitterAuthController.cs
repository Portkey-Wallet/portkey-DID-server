using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CAServer.TwitterAuth;
using CAServer.TwitterAuth.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("TwitterAuth")]
[Route("api/app/twitterAuth")]
public class TwitterAuthController : CAServerController
{
    private readonly ITwitterAuthAppService _twitterAuthAppService;
    private readonly TwitterAuthOptions _options;
    private readonly ILogger<TwitterAuthController> _logger;

    public TwitterAuthController(ITwitterAuthAppService twitterAuthAppService,
        IOptionsSnapshot<TwitterAuthOptions> options, ILogger<TwitterAuthController> logger)
    {
        _twitterAuthAppService = twitterAuthAppService;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync([FromQuery] TwitterAuthDto twitterAuthDto)
    {
        _logger.LogInformation("receive twitter callback for test, method is GET");
        if (_options.IsTest)
        {
            var data = new SortedDictionary<string, object>();
            data.Add("request.body", HttpContext.Request.QueryString.Value);
            _logger.LogInformation("receive twitter callback for test, data:{data}", JsonConvert.SerializeObject(data));
        }

        // if (!twitterAuthDto.AccessToken.IsNullOrEmpty())
        // {
        //     _logger.LogInformation("receive twitter callback, token:{token}", twitterAuthDto.AccessToken);
        //     return Redirect($"{_options.RedirectUrl}?id_token={twitterAuthDto.AccessToken}");
        // }

        await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        return Ok();
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceivePostAsync()
    {
        _logger.LogInformation("receive twitter callback for test, method is POST");
        var data = new SortedDictionary<string, object>();
        if (HttpContext.Request.Method.ToLower().Equals("post") && !HttpContext.Request.QueryString.HasValue)
        {
            var stream = HttpContext.Request.Body;
            byte[] buffer = new byte[HttpContext.Request.ContentLength.Value];
            var read = stream.Read(buffer, 0, buffer.Length);
            data.Add("request.body", Encoding.UTF8.GetString(buffer));
        }
        else
        {
            data.Add("request.body", HttpContext.Request.QueryString.Value);
        }

        _logger.LogInformation("receive twitter callback for test, data:{data}", JsonConvert.SerializeObject(data));

        return Ok();
    }
}