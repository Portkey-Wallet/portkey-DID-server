using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class TransactionReportOptions
{
    public int QueryInterval { get; set; } = 3000;
    public List<string> MethodNames { get; set; } = new();
    public List<string> ExcludeMethodNames { get; set; } = new();
}