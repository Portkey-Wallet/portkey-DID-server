using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace CAServer.Commons;

public class PagerHelper
{

    public static PageResult<T> ToPageResult<T>(PagedResultDto<T> pager)
    {
        return new PageResult<T>(pager.TotalCount, pager.Items);
    }
    
    
}


public class PageResult<T>
{
    public List<T> Data { get; set; }
    public long TotalRecordCount { get; set; }

    public PageResult(long totalCount, IEnumerable<T> items)
    {
        TotalRecordCount = totalCount;
        Data = items.ToList();
    }

}