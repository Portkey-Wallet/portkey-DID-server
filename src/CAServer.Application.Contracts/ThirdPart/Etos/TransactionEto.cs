using System;

namespace CAServer.ThirdPart.Etos;

public class TransactionEto
{
    public Guid OrderId { get; set; }
    public string RawTransaction { get; set; }
    public string PublicKey { get; set; }
}