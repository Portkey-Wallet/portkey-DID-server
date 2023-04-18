using CAServer.Grains.State;
using CAServer.Sample;
using Orleans.Providers;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain;

[StorageProvider(ProviderName = "Default")]
public class SampleGrain : Grain<SampleState>, ISampleGrain
{
    private IObjectMapper _objectMapper;

    public SampleGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public async Task<string> SayHello(string from, string to)
    {
        this.State.From = from;
        this.State.To = to;
        string message = $"{from} say hello to {to}";
        this.State.Message = message;

        await WriteStateAsync();

        return message;
    }

    public Task<TestDto> GetLastMessage()
    {
        return Task.FromResult(_objectMapper.Map<SampleState, TestDto>(State));
    }
}