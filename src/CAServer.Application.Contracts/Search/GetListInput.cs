using Nest;
using Volo.Abp.Application.Dtos;

namespace CAServer.Search;

public class GetListInput : PagedResultRequestDto
{
    public string Filter { get; set; }
    public string Sort {get; set; }
    public SortOrder SortType { get; set; }

}