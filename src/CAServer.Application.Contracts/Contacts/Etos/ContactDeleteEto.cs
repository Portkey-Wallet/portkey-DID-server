using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("ContactDeleteEto")]
public class ContactDeleteEto : ContactUpdateEto
{
}