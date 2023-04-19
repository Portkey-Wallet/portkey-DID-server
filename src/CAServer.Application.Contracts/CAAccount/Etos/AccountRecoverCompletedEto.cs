using CAServer.CAAccount.Dtos;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRecoverCompletedEto")]
public class AccountRecoverCompletedEto
{
    public RecoveryCompletedMessageDto RecoveryCompletedMessage { get; set; }
    public HubRequestContext Context { get; set; }
}