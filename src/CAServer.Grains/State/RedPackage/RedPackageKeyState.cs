namespace CAServer.Grains.State.RedPackage;

[GenerateSerializer]
public class RedPackageKeyState
{
	[Id(0)]
    public string PublicKey { get; set; } = string.Empty;
	[Id(1)]
    public string PrivateKey { get; set; } = string.Empty;
	[Id(2)]
    public long CreateTime { get; set; }
}
