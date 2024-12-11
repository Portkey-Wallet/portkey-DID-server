using Nest;
using Orleans;

namespace CAServer.FreeMint.Dtos;

[GenerateSerializer]
public class GetRecentStatusDto
{
    [Id(0)]
    public string Status { get; set; }
    [Id(1)]
    public string ItemId { get; set; }
    [Id(2)]
    public string ImageUrl { get; set; }
}