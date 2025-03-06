namespace CAServer.Grains.State.QrCode;

[GenerateSerializer]
public class QrCodeState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public DateTime ScanTime { get; set; }
}
