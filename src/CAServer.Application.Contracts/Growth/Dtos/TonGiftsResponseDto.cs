using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class TonGiftsResponseDto
{
    public List<SuccessfulUpdate> SuccessfulUpdates { get; set; }

    public List<FailedUpdate> FailedUpdates { get; set; }
    
    public string Message { get; set; }
}

public class SuccessfulUpdate : TonGiftsBase
{
}

public class FailedUpdate : TonGiftsBase
{
    public string Error { get; set; }
}

public class TonGiftsBase
{
    public string UserId { get; set; }
    public string TaskId { get; set; }
    public string Status { get; set; }
}