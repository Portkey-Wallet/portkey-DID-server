using System.Threading.Tasks;

namespace CAServer.Sample;

public interface ISampleAppService
{
    Task<string> Hello(string from, string to);
}