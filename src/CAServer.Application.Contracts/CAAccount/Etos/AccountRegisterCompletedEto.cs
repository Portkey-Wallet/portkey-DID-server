using CAServer.Dtos;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRegisterCompletedEto")]
public class AccountRegisterCompletedEto
{
    public RegisterCompletedMessageDto RegisterCompletedMessage { get; set; }
    public HubRequestContext Context { get; set; }
}