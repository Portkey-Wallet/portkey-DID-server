using System;

namespace CAServer.DataReporting.Dtos;

public class UserDeviceReportingDto : UserDeviceReportingRequestDto
{
    public Guid UserId { get; set; }
}