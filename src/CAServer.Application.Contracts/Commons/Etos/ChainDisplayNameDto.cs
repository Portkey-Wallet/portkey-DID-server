namespace CAServer.Commons.Etos;

public class ChainDisplayNameDto
{
    public string DisplayChainName { get; set; }
    public string ChainUrl { get; set; }

    private string _chainId;
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