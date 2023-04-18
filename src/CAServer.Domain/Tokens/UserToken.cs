using System;
using CAServer.Entities;

namespace CAServer.Tokens;

public class UserToken : CAServerEntity<Guid>
{
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
}
