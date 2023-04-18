using System;
using CAServer.Account;
using CAServer.Hubs;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("CAAccountEto")]
public class CAAccountEto : AccountRegisterCreateEto
{
}