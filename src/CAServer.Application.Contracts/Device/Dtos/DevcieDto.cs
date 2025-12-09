using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Device.Dtos;

public class DeviceServiceDto
{
    [Required] public List<string> Data { get; set; }
}

public class DeviceServiceResultDto
{
    public List<string> Result { get; set; }
}