using System.Threading.Tasks;

namespace CAServer.GuardiansStatistic;

public interface IGuardiansStatisticAppService
{
    Task<string> GetInfo();
}