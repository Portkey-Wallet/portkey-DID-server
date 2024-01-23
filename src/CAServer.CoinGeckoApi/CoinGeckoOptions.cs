using System.Collections.Generic;

namespace CAServer.CoinGeckoApi
{
    public class CoinGeckoOptions
    {
        public string BaseUrl { get; set; }
        public Dictionary<string, string> CoinIdMapping { get; set; }
    }
}

