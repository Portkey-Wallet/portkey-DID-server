using System;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("CreateUserEto")]
public class CreateUserEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public DateTime CreateTime { get; set; }
    
    public string ChainId { get; set; }
}