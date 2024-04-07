using System.Threading.Tasks;
using CAServer.Tab.Dtos;

namespace CAServer.Tab;

public interface ITabAppService
{
    Task CompleteAsync(TabCompleteDto input);
}