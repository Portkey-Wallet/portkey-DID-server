using Newtonsoft.Json;

namespace CAServer.Commons;

public class CoinMarketCapResponseDto<T>
{
    public CoinMarketCapResponseStatus Status { get; set; }
    
    public T Data { get; set; }
    
    public class CoinMarketCapResponseStatus
    {
        public string Timestamp { get; set; }
        
        [JsonProperty(PropertyName = "error_code")]
        public int ErrorCode { get; set; }
        
        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }
        
        public string Elapsed { get; set; }
        
        [JsonProperty(PropertyName = "credit_count")]
        public string CreditCount { get; set; }
    }

    
    
    // {
    //     "status": {
    //         "timestamp": "2018-06-02T22:51:28.209Z",
    //         "error_code": 1008,
    //         "error_message": "You've exceeded your API Key's HTTP request rate limit. Rate limits reset every minute.",
    //         "elapsed": 10,
    //         "credit_count": 0
    //     }
    // }
}