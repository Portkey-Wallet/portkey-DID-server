using System;

namespace CAServer.Account;

public class CAAccountBase
{
    public Guid Id { get; set; }
    public DateTime? CreateTime { get; set; }
    public string ChainId { get; set; }
    public Manager Manager { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }

}