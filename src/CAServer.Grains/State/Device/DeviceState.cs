namespace CAServer.Grains.State.Device;

[GenerateSerializer]
public class DeviceState
{
	[Id(0)]
    public string Salt { get; set; }
}
