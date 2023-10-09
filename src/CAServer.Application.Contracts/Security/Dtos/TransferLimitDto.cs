using System.Collections.Generic;

namespace CAServer.Security.Dtos;

public class TransferLimitDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string SingleLimit { get; set; }
    public string DefaultSingleLimit { get; set; }
    public string DailyLimit { get; set; }
    public string DefaultDailyLimit { get; set; }

    public bool Restricted { get; set; }
}

public class IndexerTransferLimitList
{
    public CaHolderTransferLimit CaHolderTransferLimit { get; set; }
}

public class CaHolderTransferLimit
{
    public long TotalRecordCount { get; set; }
    public List<TransferLimitDto> Data { get; set; }
}