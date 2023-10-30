using System;

namespace CAServer.DataReporting.Dtos;

public class ReportingDto : Reporting
{
    public Guid UserId { get; set; }
}