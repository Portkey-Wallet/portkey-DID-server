using System;
using Volo.Abp.EventBus;

namespace CAServer.Tokens.Etos;

[EventName("UserTokenDeleteEto")]
public class UserTokenDeleteEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Dtos.Token Token { get; set; }
}