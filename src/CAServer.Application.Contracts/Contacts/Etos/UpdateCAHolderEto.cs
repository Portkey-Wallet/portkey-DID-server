using System;
using Volo.Abp.EventBus;

namespace CAServer.Etos;

[EventName("UpdateCAHolderEto")]
public class UpdateCAHolderEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaAddress { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public string Avatar { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
    
}