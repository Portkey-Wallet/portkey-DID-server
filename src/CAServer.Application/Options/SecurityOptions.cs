using System.Collections.Generic;

namespace CAServer.Options;

public class SecurityOptions
{
    public Dictionary<string, long> TransferLimit { get; set; }
    public long DefaultTokenTransferLimit { get; set; }
}