using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.Security.Dtos;

public class TransferLimitDto : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string SingleLimit { get; set; }
    public string DefaultSingleLimit { get; set; }
    public string DailyLimit { get; set; }
    public string DefaultDailyLimit { get; set; }

    public bool Restricted { get; set; }
    public string ImageUrl { get; set; }
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