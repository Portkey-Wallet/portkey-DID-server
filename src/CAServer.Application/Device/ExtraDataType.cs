using System.ComponentModel;

namespace CAServer.Device;

public class ExtraDataType
{
    public long TransactionTime { get; set; }
    public string DeviceInfo { get; set; }
    public string Version { get; set; }
}