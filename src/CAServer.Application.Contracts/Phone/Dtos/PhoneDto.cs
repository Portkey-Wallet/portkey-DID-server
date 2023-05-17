using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Phone.Dtos;


public class PhoneInfoListDto
{

    public Dictionary<string, string> LocateData { get; set; }

    public List<Dictionary<string, string>> Data { get; set; }
}

public class PhoneServiceResultDto
{
    public Dictionary<string, string> MethodResponseTtl { get; set; }

}
