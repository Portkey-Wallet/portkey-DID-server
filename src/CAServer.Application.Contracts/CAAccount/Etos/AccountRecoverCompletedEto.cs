using CAServer.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRecoverCompletedEto")]
public class AccountRecoverCompletedEto : RecoveryDto
{
}