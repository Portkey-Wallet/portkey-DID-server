using Volo.Abp.EventBus;

namespace CAServer.Message.Etos;

[EventName("ScanLoginEto")]
public class ScanLoginEto : MessageBase<string>
{
}