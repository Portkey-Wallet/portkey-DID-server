using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.CryptoGift;

public interface ICryptoGiftProvider
{
    public long GetExpirationSeconds();

    public int GetTransferDelayedRetryTimes();

    public int GetTransferDelayedIntervalSeconds();
}

public class CryptoGiftProvider : ICryptoGiftProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<CryptoGiftOptions> _optionsMonitor;

    public CryptoGiftProvider(IOptionsMonitor<CryptoGiftOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }
    
    public long GetExpirationSeconds()
    {
        return _optionsMonitor.CurrentValue.ExpireSeconds;
    }

    public int GetTransferDelayedRetryTimes()
    {
        return _optionsMonitor.CurrentValue.TransferDelayedRetryTimes;
    }

    public int GetTransferDelayedIntervalSeconds()
    {
        return _optionsMonitor.CurrentValue.TransferDelayedIntervalSeconds;
    }
}