using System.Collections.Generic;

namespace CAServer.Tokens.Provider;

public class IndexerTokenApproved
{
    public CAHolderTokenApproved CaHolderTokenApproved { get; set; }
}

public class CAHolderTokenApproved
{
    public List<CAHolderTokenApprovedDto> Data { get; set; } = new();
    public long TotalRecordCount { get; set; }
}

public class CAHolderTokenApprovedDto
{
    public string ChainId { get; set; }
    public string Spender { get; set; }
    public string CaAddress { get; set; }
    public string Symbol { get; set; }
    public long BatchApprovedAmount { get; set; }
    public long UpdateTime { get; set; }
}