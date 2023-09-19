using System;
using AElf.Types;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("TransactionEto")]
public class TransactionEto
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string RawTransaction { get; set; }
    public string PublicKey { get; set; }
    public string MerchantName { get; set; }
}