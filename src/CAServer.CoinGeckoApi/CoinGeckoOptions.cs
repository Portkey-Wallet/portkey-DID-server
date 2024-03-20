using System.Collections.Generic;

namespace CAServer.CoinGeckoApi
{
    public class CoinGeckoOptions
    {
        public string BaseUrl { get; set; }
        public Dictionary<string, string> CoinIdMapping { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
        public int Timeout { get; set; } = 10000;
        public string DemoApiKey { get; set; }
    }
}