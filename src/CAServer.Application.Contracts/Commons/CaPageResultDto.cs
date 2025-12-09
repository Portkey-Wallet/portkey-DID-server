using System.Collections.Generic;

namespace CAServer.Commons;

public class CaPageResultDto<T> where T : class
{
    public List<T> Data { get; set; }
    public long TotalRecordCount { get; set; }

    public CaPageResultDto(int totalRecordCount, List<T> data)
    {
        TotalRecordCount = totalRecordCount;
        Data = data;
    }

    public CaPageResultDto()
    {
    }
}