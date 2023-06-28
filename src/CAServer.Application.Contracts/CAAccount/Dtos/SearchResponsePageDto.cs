using System.Collections.Generic;

namespace CAServer.CAAccount.Dtos;

public class SearchResponsePageDto
{
    public string Result { get; set; }
    public List<SearchResponseDto> Data { get; set; }
    public long TotalCount { get; set; }
}