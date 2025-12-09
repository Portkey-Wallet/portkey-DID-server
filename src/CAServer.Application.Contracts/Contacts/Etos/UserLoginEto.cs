using System;

namespace CAServer.Etos;

public class UserLoginEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public DateTime CreateTime { get; set; }
    
    public bool? FromCaServer { get; set; }
}