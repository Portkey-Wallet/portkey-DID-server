using CAServer.EnumType;

namespace CAServer.FreeMint.Dtos;

public class GetRecentStatusDto
{
    public FreeMintStatus Status { get; set; }
    public string ItemId { get; set; }
}