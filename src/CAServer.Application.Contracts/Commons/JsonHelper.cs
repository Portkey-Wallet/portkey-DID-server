using Newtonsoft.Json;

namespace CAServer.Commons;

public static class JsonHelper
{
    public static T DeserializeJson<T>(string jsonString)
    {
        return JsonConvert.DeserializeObject<T>(jsonString);
    }
}