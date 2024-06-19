namespace CAServer.FreeMint.Dtos;

public class GetMintInfoDto
{
    public string TokenId { get; set; }
}

public class MintCollectionInfo
{
    public string CollectionName { get; set; }
    public string ImageUrl { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
}