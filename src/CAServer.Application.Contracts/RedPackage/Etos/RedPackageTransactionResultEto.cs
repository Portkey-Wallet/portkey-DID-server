using System;

namespace CAServer.RedPackage.Etos;

public class RedPackageTransactionResultEto
{
    public string Message { get; set; }
    public string TransactionId { get; set; }
    public string TransactionResult { get; set; }
    public bool Success { get; set; }
}