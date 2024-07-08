using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Balance;

public interface IBalanceAppService
{
    public Task GetBalanceInfoAsync(string chainId);
    
    public Task<Dictionary<string,int>> GetActivityCountByDayAsync();
    Task Statistic();
}