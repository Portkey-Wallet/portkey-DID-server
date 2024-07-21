namespace CAServer.FreeMint.Dtos;

public class GetMintInfoDto
{
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public string TokenId { get; set; }
    public decimal TransactionFee { get; set; } = 0;
}