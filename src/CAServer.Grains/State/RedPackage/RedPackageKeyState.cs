namespace CAServer.Grains.State.RedPackage;

public class RedPackageKeyState
{
    public Guid Id { get; set; }

    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public long CreateTime { get; set; }
}