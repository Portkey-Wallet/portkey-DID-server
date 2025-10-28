using Newtonsoft.Json;

namespace CAServer.Verifier;

public class ShowOperationDetailsDto
{
    [JsonProperty(Order = 6,PropertyName = "Guardian Account")]
    public string GuardianAccount { get; set; }

    [JsonProperty(Order = 5,PropertyName = "Guardian Type")]
    public string GuardianType { get; set; }

    [JsonProperty(Order = 10)]
    public string Time { get; set; }

    [JsonProperty(Order = 4)]
    public string Chain { get; set; }

    [JsonProperty(Order = 2)]
    public string Token { get; set; }

    [JsonProperty(Order = 3)]
    public string Amount { get; set; }

    [JsonProperty(Order = 11)]
    public string IP { get; set; }

    [JsonProperty(Order = 1,PropertyName = "Operation Type")]
    public string OperationType { get; set; }
    
    [JsonProperty(Order = 7, PropertyName = "Transferring To Address")]
    public string ToAddress { get; set; }
    
    [JsonProperty(Order = 8, PropertyName = "Single Limit")]
    public string SingleLimit { get; set; }
    
    [JsonProperty(Order = 9, PropertyName = "Daily Limit")]
    public string DailyLimit { get; set; }
}