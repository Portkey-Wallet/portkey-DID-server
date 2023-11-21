using System.Collections.Generic;

namespace CAServer.Commons.Dtos;

public class PageResultDto<T>
{
    public PageResultDto()
    {
        Data = new List<T>();
        TotalRecordCount = 0;
    }

    public PageResultDto(List<T> data, long totalRecordCount)
    {
        Data = data;
        TotalRecordCount = totalRecordCount;
    }

    public List<T> Data { get; set; }
    public long TotalRecordCount { get; set; }
}