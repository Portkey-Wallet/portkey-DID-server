using System.Threading.Tasks;

namespace CAServer.CaHolder;

public interface ICaHolderAppService
{
    Task<string> Statistic();
    Task<string> Statistic2();
    Task<string> Sort();
}