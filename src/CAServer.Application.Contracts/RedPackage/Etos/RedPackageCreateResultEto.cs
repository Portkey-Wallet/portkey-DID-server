using System;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;

[EventName("RedPackageCreateResultEto")]
public class RedPackageCreateResultEto
{
    public Guid SessionId { get; set; }
    public string Message { get; set; }
    public string TransactionId { get; set; }
    public string TransactionResult { get; set; }
    public bool Success { get; set; }
}