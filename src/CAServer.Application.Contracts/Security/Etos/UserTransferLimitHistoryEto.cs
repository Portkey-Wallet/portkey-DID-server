using System;
using Volo.Abp.EventBus;

namespace CAServer.Security.Etos;

[EventName("UserTransferLimitHistoryEto")]
public class UserTransferLimitHistoryEto
{
    public Guid Id { get; set; }
    public string CaHash { get; set; }
    public string Symbol { get; set; }
    public string ChainId { get; set; }
}