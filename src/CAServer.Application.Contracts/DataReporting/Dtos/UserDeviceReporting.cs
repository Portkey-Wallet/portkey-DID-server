using System;

namespace CAServer.DataReporting.Dtos;

public class UserDeviceReporting : UserDeviceReportingDto
{
    public Guid UserId { get; set; }
}