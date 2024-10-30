using System.Collections.Generic;

namespace CAServer.Verifier;

public class VerifierServerBasicInfo
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string ImageUrl { get; set; }
    
    public List<string> EndPoints { get; set; }
    
    public List<string> VerifierAddresses { get; set; }
}