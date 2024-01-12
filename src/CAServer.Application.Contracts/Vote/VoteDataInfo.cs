namespace CAServer.Vote;

public class TransactionLogsInfo
{
    public string[] Indexed { get; set; }
    public string NonIndexed { get; set; }
}

public class HolderInfo
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}

public class VoteDataInfo
{
    public string TransactionId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}