using System.Threading.Tasks;

namespace CAServer.Balance;

public interface IBalanceAppService
{
    public Task GetBalanceInfoAsync(string chainId);
}