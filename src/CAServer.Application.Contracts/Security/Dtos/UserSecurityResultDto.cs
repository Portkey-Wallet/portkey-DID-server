using System.Collections.Generic;

namespace CAServer.Security.Dtos;

public class TransferLimitListResultDto
{
    public long TotalRecordCount { get; set; }
    public List<TransferLimitDto> Data { get; set; }
}