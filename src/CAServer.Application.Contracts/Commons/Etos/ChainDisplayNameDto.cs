using Orleans;

namespace CAServer.Commons.Etos;

[GenerateSerializer]
public class ChainDisplayNameDto
{
    [Id(0)]
    public string DisplayChainName { get; set; }
    [Id(1)]
    public string ChainImageUrl { get; set; }

    private string _chainId;
    [Id(2)]
    public string ChainId
    {
        get { return _chainId; }
        set
        {
            _chainId = value;
            ChainDisplayNameHelper.SetDisplayName(this, value);
        }
    }
}