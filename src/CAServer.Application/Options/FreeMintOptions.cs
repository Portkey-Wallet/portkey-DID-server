using CAServer.FreeMint.Dtos;

namespace CAServer.Options;

public class FreeMintOptions
{
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public string ContractAddress { get; set; }
    public int LimitCount { get; set; } = 5;
}