namespace CAServer.ContractEventHandler.Core.Application;

public class TransactionReportContext
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public string TransactionId { get; set; }
    public string RanTransaction { get; set; }

    public int QueryCount { get; set; }
}