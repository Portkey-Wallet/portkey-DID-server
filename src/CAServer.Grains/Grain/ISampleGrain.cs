using CAServer.Sample;
using Orleans;

namespace CAServer.Grains.Grain;

public interface ISampleGrain:IGrainWithIntegerKey
{
    Task<string> SayHello(string from, string to);

    Task<TestDto> GetLastMessage();
}