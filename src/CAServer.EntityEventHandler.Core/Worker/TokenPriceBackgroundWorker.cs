using System;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ScheduledTask;
using CAServer.Tokens.TokenPrice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.EntityEventHandler.Core.Worker;

public class TokenPriceBackgroundWorker : ScheduledTaskBase
{
    private readonly ILogger<TokenPriceBackgroundWorker> _logger;
    private readonly TokenPriceWorkerOption _tokenPriceWorkerOption;
    private readonly ITokenPriceService _tokenPriceService;

    public TokenPriceBackgroundWorker(
        ILogger<TokenPriceBackgroundWorker> logger,
        IOptionsMonitor<TokenPriceWorkerOption> tokenPriceWorkerOption,
        ITokenPriceService tokenPriceService)
    {
        _logger = logger;
        _tokenPriceService = tokenPriceService;
        _tokenPriceWorkerOption = tokenPriceWorkerOption.CurrentValue;
        Period = _tokenPriceWorkerOption.Period;
    }

    protected override async Task DoWorkAsync()
    {
        try
        {
            _logger.LogInformation("TokenPriceWorker: update token price....");
            await _tokenPriceService.RefreshCurrentPriceAsync();
            _logger.LogInformation("TokenPriceWorker: update token price finished...");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TokenPriceWorker: task error.");
        }
    }
}