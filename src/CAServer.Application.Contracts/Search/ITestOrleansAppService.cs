using System.Threading.Tasks;

namespace CAServer.Search;

public interface ITestOrleansAppService
{
    Task<string> TestOrleansAsync(string grainName, string grainKey);
}