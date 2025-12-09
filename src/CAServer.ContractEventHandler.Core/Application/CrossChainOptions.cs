using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class CrossChainOptions
{
    public Dictionary<string,long> AutoReceiveStartHeight { get; set; }
}