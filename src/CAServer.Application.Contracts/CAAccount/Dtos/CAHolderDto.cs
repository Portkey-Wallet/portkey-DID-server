using System;

namespace CAServer.Dtos;

public class CAHolderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaAddress { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public DateTime CreateTime { get; set; }
}