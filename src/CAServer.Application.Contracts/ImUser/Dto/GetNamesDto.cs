using System;

namespace CAServer.ImUser.Dto;

public class GetNamesDto
{
    public Guid PortkeyId { get; set; }
    public string RelationId { get; set; }
}