using System.Threading.Tasks;
using CAServer.Test.Dtos;

namespace CAServer.Test;

public interface ITestAppService
{
    public Task<TestResultDto> AddAsync(TestRequestDto input);
    public Task<TestResultDto> GetAsync(string id);
}
