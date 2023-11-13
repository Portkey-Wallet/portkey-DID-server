using System;

namespace CAServer.ImUser.Dto;

public class GetNamesResultDto
{
    public Guid PortkeyId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
}