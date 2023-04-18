using CAServer.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("AccountRegisterCompletedEto")]
public class AccountRegisterCompletedEto : RegisterDto
{
}