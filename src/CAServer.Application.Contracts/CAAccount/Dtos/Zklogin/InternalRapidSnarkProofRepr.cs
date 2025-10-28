using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.CAAccount.Dtos.Zklogin;

public class InternalRapidSnarkProofRepr
{
    [JsonProperty(PropertyName = "pi_a")]
    public List<string> PiA;
    
    [JsonProperty(PropertyName = "pi_b")]
    public List<List<string>> PiB;
    
    [JsonProperty(PropertyName = "pi_c")]
    public List<string> PiC;
    
    public string Protocol;
}