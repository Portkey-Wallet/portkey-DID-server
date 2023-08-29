using System.Collections.Generic;

namespace CAServer.Security.Dtos;

public class TransferLimitDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long SingleLimit { get; set; }
    public long DailyLimit { get; set; }
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
