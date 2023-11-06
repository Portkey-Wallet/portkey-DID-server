using System.Threading.Tasks;

namespace CAServer.Statistic;

public interface IStatisticAppService
{
    Task<int> GetTransferInfoAsync();
}