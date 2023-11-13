using System;
using CAServer.Common;

namespace CAServer.EventBusHandler;

public class EtoBase
{
    public string TracingId { get; set; } = DashExecutionContext.TraceIdentifier ?? Guid.NewGuid().ToString();
    
    
}