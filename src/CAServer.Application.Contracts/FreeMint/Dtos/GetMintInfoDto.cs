namespace CAServer.FreeMint.Dtos;

public class GetMintInfoDto
{
    public FreeMintCollectionInfoDto CollectionInfo { get; set; }
    public int LimitCount { get; set; }
    public bool IsLimitExceed { get; set; }
    public decimal TransactionFee { get; set; } = 0;
}