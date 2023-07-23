using Volo.Abp.EventBus;

namespace CAServer.Test.Etos;

[EventName("TestEto")]
public class TestEto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Address { get; set; }
}