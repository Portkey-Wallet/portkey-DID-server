

using System.Collections.Generic;

namespace CAServer.Verifier;

public class GetVerifierServerResponse
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string ImageUrl { get; set; }
    
    public List<string> EndPionts { get; set; }
    
    public List<string> VerifierAddressses { get; set; }


}