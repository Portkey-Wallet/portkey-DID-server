using System.Collections.Generic;
namespace CAServer.Verifier;

public class VerifierServerInfo
{
    public List<string> EndPoints { get; set; }
    public string Id { get; set; }
    
    public List<string> VerifierAddresses { get; set; }
}