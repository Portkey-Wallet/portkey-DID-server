using Nest;
using Volo.Abp.Application.Dtos;

namespace CAServer.Search;

public class GetListInput : PagedResultRequestDto
{
    public string Filter { get; set; }
    public string Sort {get; set; }
    public string DappName {get; set; }
    
}

public class SortType
{
    public string SortField { get; set; }
    public SortOrder SortOrder { get; set; }
}