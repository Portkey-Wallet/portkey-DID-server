using CAServer.Commons.Etos;

namespace CAServer.FreeMint.Dtos;

public class FreeMintCollectionInfoDto : ChainDisplayNameDto
{
    public string ChainId { get; set; }
    public string CollectionName { get; set; }
    public string ImageUrl { get; set; }
    public string Symbol { get; set; }
}