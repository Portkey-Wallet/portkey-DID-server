using System.Linq;

namespace CAServer.Commons;

public static class DeviceStringHelper
{
    public static string GetLast(string deviceString)
    {
        if (string.IsNullOrWhiteSpace(deviceString) || !deviceString.Contains(','))
        {
            return null;
        }

        return deviceString.Split(',').Last();
    }
}